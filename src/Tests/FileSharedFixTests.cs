using Common.Entities.Fixes.FileFix;
using Common.Helpers;

namespace Tests;

/// <summary>
/// Tests that use instance data and should be run in a single thread
/// </summary>
public sealed partial class FileFixTests : IDisposable
{
    private readonly string _testFixSharedZip = Path.Combine(Directory.GetCurrentDirectory(), "Resources", "test_fix_shared.zip");
    private readonly string _testFixShared2Zip = Path.Combine(Directory.GetCurrentDirectory(), "Resources", "test_fix_shared_v2.zip");

    #region Tests

    /// <summary>
    /// Install, update and uninstall fix that includes shared fix, only shared fix is updated
    /// </summary>
    [Fact]
    public async Task SharedUpdateSharedFix()
    {
        FileFixEntity sharedFixEntity = new()
        {
            Name = "shared test fix",
            Version = 1,
            Guid = Guid.Parse("C0650F19-F670-4F8A-8545-70F6C5171FA6"),
            Url = _testFixSharedZip
        };

        FileFixEntity fixEntity = new()
        {
            Name = "test fix",
            Version = 1,
            Guid = Guid.Parse("C0650F19-F670-4F8A-8545-70F6C5171FA5"),
            Url = _testFixZip,
            InstallFolder = "install folder",
            SharedFix = sharedFixEntity,
            SharedFixGuid = sharedFixEntity.Guid,
            SharedFixInstallFolder = "shared install folder"
        };

        await InstallAsync(fixEntity).ConfigureAwait(true);

        await UpdateSharedFixAsync(fixEntity).ConfigureAwait(true);

        Uninstall(fixEntity);
    }

    /// <summary>
    /// Install, update and uninstall fix that includes shared fix, only main fix is updated
    /// </summary>
    [Fact]
    public async Task SharedUpdateMainFix()
    {
        FileFixEntity sharedFixEntity = new()
        {
            Name = "shared test fix",
            Version = 1,
            Guid = Guid.Parse("C0650F19-F670-4F8A-8545-70F6C5171FA6"),
            Url = _testFixSharedZip
        };

        FileFixEntity fixEntity = new()
        {
            Name = "test fix",
            Version = 1,
            Guid = Guid.Parse("C0650F19-F670-4F8A-8545-70F6C5171FA5"),
            Url = _testFixZip,
            InstallFolder = "install folder",
            SharedFix = sharedFixEntity,
            SharedFixGuid = sharedFixEntity.Guid,
            SharedFixInstallFolder = "shared install folder"
        };

        await InstallAsync(fixEntity).ConfigureAwait(true);

        await UpdateMainFixAsync(fixEntity).ConfigureAwait(true);

        Uninstall(fixEntity);
    }

    #endregion Tests

    private async Task InstallAsync(FileFixEntity fixEntity)
    {
        await _fixManager.InstallFixAsync(_gameEntity, fixEntity, null, true, new()).ConfigureAwait(true);

        Assert.True(File.Exists(Path.Combine("game", "shared install folder", "shared fix file.txt")));

        var installedActual = File.ReadAllText(Path.Combine(_gameEntity.InstallDir, Consts.BackupFolder, fixEntity.Guid.ToString() + ".json"));
        var installedExpected = $@"{{
  ""$type"": ""FileFix"",
  ""BackupFolder"": ""test_fix"",
  ""FilesList"": [
    ""install folder{SeparatorForJson}start game.exe"",
    ""install folder{SeparatorForJson}subfolder{SeparatorForJson}file.txt""
  ],
  ""InstalledSharedFix"": {{
    ""BackupFolder"": null,
    ""FilesList"": [
      ""shared install folder{SeparatorForJson}"",
      ""shared install folder{SeparatorForJson}shared fix file.txt""
    ],
    ""InstalledSharedFix"": null,
    ""WineDllOverrides"": null,
    ""GameId"": 1,
    ""Guid"": ""c0650f19-f670-4f8a-8545-70f6c5171fa6"",
    ""Version"": 1
  }},
  ""WineDllOverrides"": null,
  ""GameId"": 1,
  ""Guid"": ""c0650f19-f670-4f8a-8545-70f6c5171fa5"",
  ""Version"": 1
}}";

        Assert.Equal(installedExpected, installedActual);
    }

    private async Task UpdateMainFixAsync(FileFixEntity fixEntity)
    {
        fixEntity.Version = 2;
        fixEntity.Url = _testFixV2Zip;

        await _fixManager.UpdateFixAsync(_gameEntity, fixEntity, null, true, new()).ConfigureAwait(true);

        Assert.True(File.Exists(Path.Combine("game", "shared install folder", "shared fix file.txt")));

        var newFileActual = File.ReadAllText(Path.Combine("game", "install folder", "start game.exe"));
        var newFileExpected = "fix_v2";

        var installedActual = File.ReadAllText(Path.Combine(_gameEntity.InstallDir, Consts.BackupFolder, fixEntity.Guid.ToString() + ".json"));
        var installedExpected = $@"{{
  ""$type"": ""FileFix"",
  ""BackupFolder"": ""test_fix"",
  ""FilesList"": [
    ""install folder{SeparatorForJson}start game.exe""
  ],
  ""InstalledSharedFix"": {{
    ""BackupFolder"": null,
    ""FilesList"": [
      ""shared install folder{SeparatorForJson}"",
      ""shared install folder{SeparatorForJson}shared fix file.txt""
    ],
    ""InstalledSharedFix"": null,
    ""WineDllOverrides"": null,
    ""GameId"": 1,
    ""Guid"": ""c0650f19-f670-4f8a-8545-70f6c5171fa6"",
    ""Version"": 1
  }},
  ""WineDllOverrides"": null,
  ""GameId"": 1,
  ""Guid"": ""c0650f19-f670-4f8a-8545-70f6c5171fa5"",
  ""Version"": 2
}}";

        Assert.Equal(newFileExpected, newFileActual);
        Assert.Equal(installedExpected, installedActual);
    }

    private async Task UpdateSharedFixAsync(FileFixEntity fixEntity)
    {
        FileFixEntity sharedFixEntity2 = new()
        {
            Name = "shared test fix",
            Version = 2,
            Guid = Guid.Parse("C0650F19-F670-4F8A-8545-70F6C5171FA6"),
            Url = _testFixShared2Zip
        };

        fixEntity.SharedFix = sharedFixEntity2;

        await _fixManager.UpdateFixAsync(_gameEntity, fixEntity, null, true, new()).ConfigureAwait(true);

        Assert.True(File.Exists(Path.Combine("game", "shared install folder", "shared fix file 2.txt")));

        var installedActual = File.ReadAllText(Path.Combine(_gameEntity.InstallDir, Consts.BackupFolder, fixEntity.Guid.ToString() + ".json"));
        var installedExpected = $@"{{
  ""$type"": ""FileFix"",
  ""BackupFolder"": ""test_fix"",
  ""FilesList"": [
    ""install folder{SeparatorForJson}start game.exe"",
    ""install folder{SeparatorForJson}subfolder{SeparatorForJson}file.txt""
  ],
  ""InstalledSharedFix"": {{
    ""BackupFolder"": null,
    ""FilesList"": [
      ""shared install folder{SeparatorForJson}"",
      ""shared install folder{SeparatorForJson}shared fix file 2.txt"",
      ""shared install folder{SeparatorForJson}shared fix file.txt""
    ],
    ""InstalledSharedFix"": null,
    ""WineDllOverrides"": null,
    ""GameId"": 1,
    ""Guid"": ""c0650f19-f670-4f8a-8545-70f6c5171fa6"",
    ""Version"": 2
  }},
  ""WineDllOverrides"": null,
  ""GameId"": 1,
  ""Guid"": ""c0650f19-f670-4f8a-8545-70f6c5171fa5"",
  ""Version"": 1
}}";

        Assert.Equal(installedExpected, installedActual);
    }

    private void Uninstall(FileFixEntity fixEntity)
    {
        //uninstall
        _fixManager.UninstallFix(_gameEntity, fixEntity);

        Assert.False(Directory.Exists(Path.Combine("game", "shared install folder")));

        CheckOriginalFiles();

        Assert.False(File.Exists(Path.Combine(_gameEntity.InstallDir, Consts.BackupFolder, fixEntity.Guid.ToString() + ".json")));
    }
}

