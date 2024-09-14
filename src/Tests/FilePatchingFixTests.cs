using Common.Entities.Fixes.FileFix;
using Common.Helpers;

namespace Tests;

/// <summary>
/// Tests that use instance data and should be run in a single thread
/// </summary>
public sealed partial class FileFixTests
{
    private readonly string _testFixPatchZip = Path.Combine(Directory.GetCurrentDirectory(), "Resources", "test_fix_patch.zip");

    /// <summary>
    /// Install and uninstall fix that includes octodiff patch
    /// </summary>
    [Fact]
    public async Task InstallFixWithPatching()
    {
        FileFixEntity fixEntity = new()
        {
            Name = "test fix with patch",
            Version = 1,
            Guid = Guid.Parse("C0650F19-F670-4F8A-8545-70F6C5171FA5"),
            Url = _testFixPatchZip,
            InstallFolder = "install folder",
            FilesToPatch = ["install folder\\start game.exe"]
        };

        _ = await _fixManager.InstallFixAsync(_gameEntity, fixEntity, null, true, new()).ConfigureAwait(true);

        var installedActual = File.ReadAllText(Path.Combine(_gameEntity.InstallDir, Consts.BackupFolder, fixEntity.Guid.ToString() + ".json"));
        var installedExpected = $@"{{
  ""$type"": ""FileFix"",
  ""BackupFolder"": ""test_fix_with_patch"",
  ""FilesList"": [
    ""install folder{Helpers.SeparatorForJson}start game.exe.octodiff""
  ],
  ""InstalledSharedFix"": null,
  ""WineDllOverrides"": null,
  ""GameId"": 1,
  ""Guid"": ""c0650f19-f670-4f8a-8545-70f6c5171fa5"",
  ""Version"": 1
}}";

        var exeActual = File.ReadAllText(Path.Combine("game", "install folder", "start game.exe"));
        var exeExpected = "original_patched";

        Assert.Equal(installedExpected, installedActual);
        Assert.Equal(exeExpected, exeActual);

        UninstallFix(fixEntity);
    }
}

