using Common.Client.FixTools;
using Common.Client.FixTools.RegistryFix;
using Common.Client.Providers;
using Common.Client.Providers.Interfaces;
using Common.Entities;
using Common.Entities.Fixes.RegistryFix;
using Common.Enums;
using Common.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Moq;

namespace Tests;

/// <summary>
/// Tests that use instance data and should be run in a single thread
/// </summary>
[Collection("Sync")]
public sealed class RegistryFixTests : IDisposable
{
    private readonly FixManager _fixManager;

    private readonly GameEntity _gameEntity = new()
    {
        Id = 1,
        Name = "test game",
        InstallDir = Helpers.GameDir,
        Icon = string.Empty
    };

    private readonly RegistryFixEntity _fixEntity = new()
    {
        Name = "test fix",
        Version = "1.0",
        Guid = Guid.Parse("C0650F19-F670-4F8A-8545-70F6C5171FA5"),
        Entries =
        [
            new()
            {
                Key = "HKEY_CURRENT_USER\\" + Helpers.RegKey,
                ValueName = "{gamefolder}\\" + Helpers.GameExe,
                NewValueData = "~ RUNASADMIN",
                ValueType = RegistryValueTypeEnum.String,
            }
        ],
        SupportedOSes = OSEnum.Windows
    };

    #region Test Preparations

    public RegistryFixTests()
    {
        if (!OperatingSystem.IsWindows())
        {
            _fixManager = null!;
            return;
        }

        _ = Directory.CreateDirectory(Helpers.TestFolder);
        Directory.SetCurrentDirectory(Helpers.TestFolder);

        InstalledFixesProvider installedFixesProvider = new(new Mock<IGamesProvider>().Object, new Mock<ILogger>().Object);
        _ = installedFixesProvider.GetInstalledFixesListAsync();

        RegistryFixInstaller registryFixInstaller = new(new Mock<ILogger>().Object);
        RegistryFixUninstaller registryFixUninstaller = new();

        _fixManager = new(
            null!,
            null!,
            null!,
            registryFixInstaller,
            registryFixUninstaller,
            null!,
            null!,
            null!,
            null!,
            installedFixesProvider,
            new Mock<ILogger>().Object
            );
    }

    public void Dispose()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using (var key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\AppCompatFlags", true))
        {
            if (key is null)
            {
                return;
            }

            key.DeleteSubKey("Layers_test", false);
        }

        Directory.SetCurrentDirectory(Helpers.RootFolder);

        if (Directory.Exists(Helpers.TestFolder))
        {
            Directory.Delete(Helpers.TestFolder, true);
        }
    }

    #endregion Test Preparations

    #region Tests

    [Fact]
    public async Task InstallUninstallFixTest()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        //Install Fix
        _ = await _fixManager.InstallFixAsync(_gameEntity, _fixEntity, null, true, CancellationToken.None).ConfigureAwait(true);

        //Check if registry value is created
        using (var key = Registry.CurrentUser.OpenSubKey(Helpers.RegKey, true))
        {
            Assert.NotNull(key);

            var value = (string?)key.GetValue(Helpers.GameDir + "\\" + Helpers.GameExe, null);

            if (value is null)
            {
                _ = _fixManager.UninstallFix(_gameEntity, _fixEntity);
                Assert.Fail();
            }

            Assert.Equal(_fixEntity.Entries.First().NewValueData, value);
        }

        //Check created json
        var installedActual = File.ReadAllText(Path.Combine(_gameEntity.InstallDir, Consts.BackupFolder, _fixEntity.Guid.ToString() + ".json"));
        var installedExpected = $@"{{
  ""$type"": ""RegistryFix"",
  ""Entries"": [
    {{
      ""Key"": ""HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\AppCompatFlags\\Layers_test"",
      ""ValueName"": ""{Helpers.TestFolder.Replace("\\", Helpers.SeparatorForJson)}{Helpers.SeparatorForJson}game_dir{Helpers.SeparatorForJson}game exe.exe"",
      ""ValueType"": ""String"",
      ""OriginalValue"": null
    }}
  ],
  ""GameId"": 1,
  ""Guid"": ""c0650f19-f670-4f8a-8545-70f6c5171fa5"",
  ""Version"": ""1.0""
}}";

        Assert.Equal(installedExpected, installedActual);

        //Uninstall fix
        _ = _fixManager.UninstallFix(_gameEntity, _fixEntity);

        //Check if registry value is removed
        using (var key = Registry.CurrentUser.OpenSubKey(Helpers.RegKey, true))
        {
            Assert.NotNull(key);

            var value = key.GetValue(Helpers.GameDir + "\\" + Helpers.GameExe, null);

            Assert.Null(value);
        }
    }

    [Fact]
    public async Task InstallUninstallReplaceFixTest()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        const string OldValue = "OLD VALUE";

        //Preparations
        using (var key = Registry.CurrentUser.CreateSubKey(Helpers.RegKey))
        {
            key.SetValue(Helpers.GameDir + "\\" + Helpers.GameExe, OldValue);
        }

        //Install Fix
        _ = await _fixManager.InstallFixAsync(_gameEntity, _fixEntity, null, true, CancellationToken.None).ConfigureAwait(true);

        //Check created json
        var installedActual = File.ReadAllText(Path.Combine(_gameEntity.InstallDir, Consts.BackupFolder, _fixEntity.Guid.ToString() + ".json"));
        var installedExpected = $@"{{
  ""$type"": ""RegistryFix"",
  ""Entries"": [
    {{
      ""Key"": ""HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\AppCompatFlags\\Layers_test"",
      ""ValueName"": ""{Helpers.TestFolder.Replace("\\", Helpers.SeparatorForJson)}{Helpers.SeparatorForJson}game_dir{Helpers.SeparatorForJson}game exe.exe"",
      ""ValueType"": ""String"",
      ""OriginalValue"": ""OLD VALUE""
    }}
  ],
  ""GameId"": 1,
  ""Guid"": ""c0650f19-f670-4f8a-8545-70f6c5171fa5"",
  ""Version"": ""1.0""
}}";

        Assert.Equal(installedExpected, installedActual);

        //Check if registry value is set
        using (var key = Registry.CurrentUser.OpenSubKey(Helpers.RegKey, true))
        {
            Assert.NotNull(key);

            var value = (string?)key.GetValue(Helpers.GameDir + "\\" + Helpers.GameExe, null);

            if (value is null)
            {
                _ = _fixManager.UninstallFix(_gameEntity, _fixEntity);
                Assert.Fail();
            }

            Assert.Equal(_fixEntity.Entries.First().NewValueData, value);
        }

        //Uninstall fix
        _ = _fixManager.UninstallFix(_gameEntity, _fixEntity);

        //Check if registry value is reverted
        using (var key = Registry.CurrentUser.OpenSubKey(Helpers.RegKey, true))
        {
            Assert.NotNull(key);

            var value = (string?)key.GetValue(Helpers.GameDir + "\\" + Helpers.GameExe, null);

            Assert.Equal(OldValue, value);
        }
    }

    #endregion Tests
}
