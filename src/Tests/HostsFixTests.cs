using Common.Axiom.Entities;
using Common.Axiom.Entities.Fixes.HostsFix;
using Common.Axiom.Enums;
using Common.Axiom.Helpers;
using Common.Client.FixTools;
using Common.Client.FixTools.HostsFix;
using Common.Client.Providers;
using Common.Client.Providers.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace Tests;

/// <summary>
/// Tests that use instance data and should be run in a single thread
/// </summary>
[Collection("Sync")]
public sealed class HostsFixTests : IDisposable
{
    private readonly string _hostsFilePath;

    private readonly FixManager _fixManager;

    private readonly GameEntity _gameEntity = new()
    {
        Id = 1,
        Name = "test game",
        InstallDir = "C:\\games\\test game\\",
        Icon = string.Empty,
        BuildId = 1,
        TargetBuildId = 1,
    };

    private readonly HostsFixEntity _fixEntity = new()
    {
        Name = "test fix",
        Version = "1.0",
        Guid = Guid.Parse("C0650F19-F670-4F8A-8545-70F6C5171FA5"),
        Entries = ["123 added entry "],
        SupportedOSes = OSEnum.Windows
    };

    #region Test Preparations

    public HostsFixTests()
    {
        if (!OperatingSystem.IsWindows())
        {
            _hostsFilePath = string.Empty;
            _fixManager = null!;
            return;
        }

        _hostsFilePath = Path.Combine(Helpers.TestFolder, "hosts");

        if (Directory.Exists(Helpers.TestFolder))
        {
            Directory.Delete(Helpers.TestFolder, true);
        }

        _ = Directory.CreateDirectory(Helpers.TestFolder);
        Directory.SetCurrentDirectory(Helpers.TestFolder);

        File.Copy(
            Path.Combine(Helpers.RootFolder, "Resources\\hosts"),
            _hostsFilePath,
            true
            );

        HostsFixInstaller hostsFixInstaller = new();
        HostsFixUninstaller hostsFixUninstaller = new();
        InstalledFixesProvider installedFixesProvider = new(new Mock<IGamesProvider>().Object, new Mock<ILogger>().Object);
        _ = installedFixesProvider.GetInstalledFixesListAsync();

        _fixManager = new(
        null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            hostsFixInstaller,
            hostsFixUninstaller,
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

        Directory.SetCurrentDirectory(Helpers.RootFolder);

        if (Directory.Exists(Helpers.TestFolder))
        {
            Directory.Delete(Helpers.TestFolder, true);
        }
    }

    #endregion Test Preparations

    #region Tests

    [Fact]
    public async Task InstallUninstallHostsFixTest()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        //Install Fix
        var installResult = await _fixManager.InstallFixAsync(_gameEntity, _fixEntity, null, true, new(), _hostsFilePath).ConfigureAwait(true);

        Assert.True(installResult.IsSuccess);

        CheckInstalled(_fixEntity.Guid.ToString());

        //Uninstall Fix
        var uninstallResult = _fixManager.UninstallFix(_gameEntity, _fixEntity, _hostsFilePath);

        Assert.True(uninstallResult.IsSuccess);

        CheckUninstalled(_fixEntity.Guid.ToString());
    }

    #endregion Tests

    #region Private Methods

    private void CheckInstalled(string fixGuid)
    {
        var hostsActual1 = File.ReadAllText(_hostsFilePath);
        var hostsExpected1 = $@"# Copyright (c) 1993-2009 Microsoft Corp.

0.0.0.0 google.com

0.0.0.0 test.site
0.0.0.0 testtesttest # comment

123 added entry  #c0650f19-f670-4f8a-8545-70f6c5171fa5";

        var instActual1 = File.ReadAllText(Path.Combine(_gameEntity.InstallDir, CommonConstants.BackupFolder, fixGuid + ".json"));
        var instExpected1 = $@"{{
  ""$type"": ""HostsFix"",
  ""Entries"": [
    ""123 added entry ""
  ],
  ""GameId"": 1,
  ""Guid"": ""c0650f19-f670-4f8a-8545-70f6c5171fa5"",
  ""Version"": ""1.0""
}}";

        Assert.Equal(hostsExpected1, hostsActual1);
        Assert.Equal(instExpected1, instActual1);
    }

    private void CheckUninstalled(string fixGuid)
    {
        var hostsActual2 = File.ReadAllText(_hostsFilePath);
        var hostsExpected2 = $@"# Copyright (c) 1993-2009 Microsoft Corp.

0.0.0.0 google.com

0.0.0.0 test.site
0.0.0.0 testtesttest # comment
";

        Assert.Equal(hostsExpected2, hostsActual2);
        Assert.False(File.Exists(Path.Combine(_gameEntity.InstallDir, CommonConstants.BackupFolder, fixGuid + ".json")));
    }

    #endregion Private Methods
}

