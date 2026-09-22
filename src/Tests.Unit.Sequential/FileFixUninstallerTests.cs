using Common.Client.FixTools.FileFix;

namespace Tests.Unit.Sequential;

/// <summary>
/// Tests for <see cref="FileFixUninstaller"/>
/// </summary>
public sealed class FileFixUninstallerTests : IDisposable
{
    private readonly string _tempFolder;

    public FileFixUninstallerTests()
    {
        _tempFolder = Path.Combine(Path.GetTempPath(), "superheater_uninstaller_" + Guid.NewGuid().ToString("N"));
        _ = Directory.CreateDirectory(_tempFolder);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempFolder))
        {
            Directory.Delete(_tempFolder, true);
        }
    }

    /// <summary>
    /// Removing dll overrides doesn't throw when the section is absent
    /// </summary>
    [Fact]
    public void RemoveWineDllOverridesWithoutSection()
    {
        var file = CreateRegFile("""
            [Software\\Wine]
            "Version"="win10"

            """);

        FileFixUninstaller.RemoveWineDllOverridesFromFile(file, ["\"dll1\"=\"n,b\""]);

        Assert.Contains(@"[Software\\Wine]", File.ReadAllLines(file));
    }

    /// <summary>
    /// Removing dll overrides doesn't throw when the file is missing
    /// </summary>
    [Fact]
    public void RemoveWineDllOverridesWithoutFile()
    {
        var file = Path.Combine(_tempFolder, "user.reg");

        FileFixUninstaller.RemoveWineDllOverridesFromFile(file, ["\"dll1\"=\"n,b\""]);

        Assert.False(File.Exists(file));
    }

    /// <summary>
    /// Removing dll overrides deletes only the added lines from the section
    /// </summary>
    [Fact]
    public void RemoveWineDllOverridesRemovesLines()
    {
        var file = CreateRegFile("""
            [Software\\Wine\\DllOverrides]
            "dll1"="n,b"
            "dll2"="n,b"
            "keep"=""

            [Software\\Wine]
            "Version"="win10"

            """);

        FileFixUninstaller.RemoveWineDllOverridesFromFile(file, ["\"dll1\"=\"n,b\"", "\"dll2\"=\"n,b\""]);

        string[] expected =
        [
            @"[Software\\Wine\\DllOverrides]",
            "\"keep\"=\"\"",
            string.Empty,
            @"[Software\\Wine]",
            "\"Version\"=\"win10\"",
        ];

        Assert.Equal(expected, File.ReadAllLines(file));
    }

    private string CreateRegFile(string content)
    {
        var file = Path.Combine(_tempFolder, "user.reg");
        File.WriteAllText(file, content);
        return file;
    }
}
