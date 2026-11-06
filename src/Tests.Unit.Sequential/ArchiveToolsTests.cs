using System.IO.Compression;
using Common.Client;
using Common.Client.FilesTools;

namespace Tests.Unit.Sequential;

public sealed class ArchiveToolsTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "superheater-archive-" + Guid.NewGuid().ToString("N"));
    private readonly ProgressReport _progress = new();

    public ArchiveToolsTests()
    {
        _ = Directory.CreateDirectory(_root);
    }

    [Theory]
    [InlineData("../outside.txt")]
    [InlineData("..\\outside.txt")]
    [InlineData("/outside.txt")]
    [InlineData("C:/outside.txt")]
    [InlineData("C:outside.txt")]
    [InlineData("//server/share/file.txt")]
    [InlineData("safe/../../destination-sibling/outside.txt")]
    [InlineData("file.txt:stream")]
    public async Task UnsafeEntryRejectsEntireArchiveBeforeWriting(string unsafePath)
    {
        var archive = CreateZip("safe.txt", unsafePath);
        var destination = Path.Combine(_root, "destination");
        var tools = new ArchiveTools(_progress);

        _ = Assert.Throws<InvalidDataException>(() => tools.GetListOfFilesInArchive(archive, destination, null, null));
        _ = await Assert.ThrowsAsync<InvalidDataException>(() => tools.UnpackArchiveAsync(archive, destination, null));

        Assert.False(Directory.Exists(destination));
        Assert.False(File.Exists(Path.Combine(_root, "outside.txt")));
        Assert.Equal(string.Empty, _progress.OperationMessage);
    }

    [Fact]
    public async Task VariantRemovesOnlyLeadingPrefixAndUsesSameInventory()
    {
        var archive = CreateZip("chosen/", "chosen/chosen/file.txt", "chosen-other/ignored.txt", "Chosen/ignored.txt");
        var destination = Path.Combine(_root, "destination");
        var tools = new ArchiveTools(_progress);

        var inventory = tools.GetListOfFilesInArchive(archive, destination, "install", "chosen");
        await tools.UnpackArchiveAsync(archive, destination, "chosen");

        Assert.Contains(Path.Combine("install", "chosen", "file.txt"), inventory.Keys);
        Assert.Equal("payload", await File.ReadAllTextAsync(Path.Combine(destination, "chosen", "file.txt")));
        Assert.Single(Directory.GetFiles(destination, "*", SearchOption.AllDirectories));
    }

    [Theory]
    [InlineData("file.txt", "file.txt")]
    [InlineData("parent", "parent/child.txt")]
    public async Task ConflictingEntriesAreRejectedBeforeWriting(string first, string second)
    {
        var archive = CreateZip(first, second);
        var destination = Path.Combine(_root, "destination");
        _ = await Assert.ThrowsAsync<InvalidDataException>(() => new ArchiveTools(_progress).UnpackArchiveAsync(archive, destination, null));
        Assert.False(Directory.Exists(destination));
    }

    [Fact]
    public async Task ArchiveSymlinkIsRejectedBeforeWriting()
    {
        var archive = CreateZip("safe.txt", "link");
        using (var zip = ZipFile.Open(archive, ZipArchiveMode.Update))
        {
            zip.GetEntry("link")!.ExternalAttributes = unchecked((int)0xA1FF0000);
        }

        var destination = Path.Combine(_root, "destination");
        _ = await Assert.ThrowsAsync<InvalidDataException>(() => new ArchiveTools(_progress).UnpackArchiveAsync(archive, destination, null));
        Assert.False(Directory.Exists(destination));
    }

    [Fact]
    public async Task ExistingFilesystemLinkIsRejected()
    {
        var outside = Path.Combine(_root, "outside");
        _ = Directory.CreateDirectory(outside);
        var destination = Path.Combine(_root, "destination");
        _ = Directory.CreateDirectory(destination);
        var link = Path.Combine(destination, "link");
        try
        {
            _ = Directory.CreateSymbolicLink(link, outside);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException || (ex is IOException && (ex.HResult & 0xFFFF) == 1314))
        {
            Assert.Skip("Creating symbolic links requires Windows Developer Mode or elevation.");
        }

        var archive = CreateZip("safe.txt", "link/file.txt");
        _ = await Assert.ThrowsAsync<InvalidDataException>(() => new ArchiveTools(_progress).UnpackArchiveAsync(archive, destination, null));
        Assert.Empty(Directory.GetFiles(outside));
        Assert.False(File.Exists(Path.Combine(destination, "safe.txt")));
        Directory.Delete(link);
    }

    [Fact]
    public async Task ExplicitExternalRootAndBackslashEntriesAreSupported()
    {
        var archive = CreateZip("nested\\file.txt");
        var destination = Path.Combine(_root, "external");
        var tools = new ArchiveTools(_progress);
        var inventory = tools.GetListOfFilesInArchive(archive, destination, destination, null);
        await tools.UnpackArchiveAsync(archive, destination, null);
        Assert.Contains(Path.Combine(destination, "nested", "file.txt"), inventory.Keys);
        Assert.True(File.Exists(Path.Combine(destination, "nested", "file.txt")));
    }

    [Fact]
    public async Task InstallerRejectsUnsafeArchiveBeforeMovingOriginalsOrDeletingBackups()
    {
        var archive = CreateZip("safe.txt", "../outside.txt");
        var gameFolder = Path.Combine(_root, "game");
        var backupFolder = Path.Combine(gameFolder, ".superheater", "test_fix");
        _ = Directory.CreateDirectory(backupFolder);
        var original = Path.Combine(gameFolder, "original.txt");
        var backup = Path.Combine(backupFolder, "preserved.txt");
        await File.WriteAllTextAsync(original, "original");
        await File.WriteAllTextAsync(backup, "backup");
        var game = new Common.Axiom.Entities.GameEntity
        {
            Id = 1, Name = "test", InstallDir = gameFolder, Icon = string.Empty, BuildId = 1, TargetBuildId = 1
        };
        var fix = Common.Axiom.Entities.Fixes.FileFix.FileFixEntity.CreateBlank();
        fix.Name = "test fix";
        fix.Url = new Uri(archive).AbsoluteUri;
        fix.FilesToDelete = ["original.txt"];
        var installer = new Common.Client.FixTools.FileFix.FileFixInstaller(
            new Moq.Mock<Common.Axiom.IConfigProvider>().Object, new ArchiveTools(_progress),
            new Moq.Mock<Common.Client.FilesTools.Interfaces.IFilesDownloader>().Object, _progress,
            new Moq.Mock<Microsoft.Extensions.Logging.ILogger>().Object,
            new Moq.Mock<Api.Axiom.Interfaces.IApiInterface>().Object,
            new Moq.Mock<Common.Client.Providers.Interfaces.IFixesProvider>().Object);

        _ = await Assert.ThrowsAsync<InvalidDataException>(() => installer.InstallFixAsync(game, fix, null, true, CancellationToken.None));

        Assert.Equal("original", await File.ReadAllTextAsync(original));
        Assert.Equal("backup", await File.ReadAllTextAsync(backup));
    }
    [Fact]
    public async Task SevenZipArchiveUsesTheSameValidatedInventory()
    {
        var archive = Path.Combine(_root, "sample.7z");
        await File.WriteAllBytesAsync(archive, Convert.FromBase64String("N3q8ryccAATs0UV5CwAAAAAAAABaAAAAAAAAAJdJJ54BAAZwYXlsb2FkAAEEBgABCQsABwsBAAEhIQEADAcACAoBFWosQgAABQEZDAAAAAAAAAAAAAAAABETAGYAaQBsAGUALgB0AHgAdAAAABkAFAoBAAodjCUgbd0BFQYBACAAAAAAAA=="));
        var destination = Path.Combine(_root, "destination");
        var tools = new ArchiveTools(_progress);
        var inventory = tools.GetListOfFilesInArchive(archive, destination, null, null);
        await tools.UnpackArchiveAsync(archive, destination, null);
        Assert.Contains("file.txt", inventory.Keys);
        Assert.Equal("payload", await File.ReadAllTextAsync(Path.Combine(destination, "file.txt")));
    }
    private string CreateZip(params string[] entries)
    {
        var path = Path.Combine(_root, Guid.NewGuid().ToString("N") + ".zip");
        using var zip = ZipFile.Open(path, ZipArchiveMode.Create);
        foreach (var name in entries)
        {
            var entry = zip.CreateEntry(name);
            if (!name.EndsWith('/'))
            {
                using var writer = new StreamWriter(entry.Open());
                writer.Write("payload");
            }
        }

        return path;
    }

    public void Dispose()
    {
        Directory.Delete(_root, true);
    }
}
