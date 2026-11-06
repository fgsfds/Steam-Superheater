using Common.Axiom.Entities;
using Common.Axiom.Entities.Fixes.FileFix;
using Common.Client;
using Common.Client.Providers;
using Common.Client.Providers.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace Tests.Unit.Sequential;

public sealed class InstalledFixesProviderTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "superheater-state-" + Guid.NewGuid().ToString("N"));
    private readonly GameEntity _game;
    private readonly InstalledFixesProvider _provider;

    public InstalledFixesProviderTests()
    {
        _game = new() { Id = 1, Name = "game", InstallDir = _root, Icon = "", BuildId = 1, TargetBuildId = 1 };
        _ = Directory.CreateDirectory(Path.Combine(_root, ClientConstants.BackupFolder));
        var games = new Mock<IGamesProvider>();
        _ = games.Setup(x => x.GetGamesListAsync(false)).ReturnsAsync([_game]);
        _provider = new(games.Object, new Mock<ILogger>().Object);
    }

    [Fact]
    public async Task FailedReplacementKeepsCacheAndPreviousRecord()
    {
        _ = await _provider.GetInstalledFixesListAsync();
        var record = Record("1");
        Assert.True(_provider.CreateInstalledJson(_game, record).IsSuccess);
        var path = RecordPath(record.Guid);
        using (var locked = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            if (OperatingSystem.IsWindows())
            {
                Assert.False(_provider.CreateInstalledJson(_game, Record("2", record.Guid)).IsSuccess);
                Assert.Equal("1", (await _provider.GetInstalledFixesListAsync()).Single().Version);
                Assert.False(_provider.RemoveInstalledJson(_game, record.Guid).IsSuccess);
                Assert.Single(await _provider.GetInstalledFixesListAsync());
            }
        }
        Assert.Empty(Directory.GetFiles(Path.GetDirectoryName(path)!, "*.tmp-*"));
    }

    [Fact]
    public async Task FailedCreateDoesNotPublishCacheEntry()
    {
        _ = await _provider.GetInstalledFixesListAsync();
        var record = Record("1");
        _ = Directory.CreateDirectory(RecordPath(record.Guid));
        Assert.False(_provider.CreateInstalledJson(_game, record).IsSuccess);
        Assert.Empty(await _provider.GetInstalledFixesListAsync());
    }

    [Fact]
    public async Task CorruptRecordIsPreservedInQuarantine()
    {
        var path = RecordPath(Guid.NewGuid());
        await File.WriteAllTextAsync(path, "{broken");
        Assert.Empty(await _provider.GetInstalledFixesListAsync());
        var quarantine = Directory.GetFiles(Path.GetDirectoryName(path)!, "*.corrupt-*").Single();
        Assert.Equal("{broken", await File.ReadAllTextAsync(quarantine));
    }

    [Fact]
    public async Task LegacyMigrationPreservesOriginalAndVersion()
    {
        var guid = Guid.NewGuid();
        var path = RecordPath(guid);
        var text = $$"""{"$type":"FileFix","GameId":1,"Guid":"{{guid}}","Version":1,"VersionStr":"2.3"}""";
        await File.WriteAllTextAsync(path, text);
        Assert.Equal("2.3", (await _provider.GetInstalledFixesListAsync()).Single().Version);
        Assert.Equal(text, await File.ReadAllTextAsync(Directory.GetFiles(Path.GetDirectoryName(path)!, "*.legacy-*").Single()));
    }

    /// <summary>Legacy migration handles every shared-fix level and preserves the original record.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LegacyMigrationIncludesNestedSharedFixes(bool modernParent)
    {
        var guid = Guid.NewGuid();
        var sharedGuid = Guid.NewGuid();
        var nestedGuid = Guid.NewGuid();
        var path = RecordPath(guid);
        var version = modernParent ? "\"Version\":\"2.3\"" : "\"Version\":1,\"VersionStr\":\"2.3\"";
        var text = $$"""
            {
              "$type":"FileFix", "GameId":1, "Guid":"{{guid}}", {{version}},
              "InstalledSharedFix": {
                "GameId":1, "Guid":"{{sharedGuid}}", "Version":1, "VersionStr":"4.5",
                "BackupFolder":"shared-backup", "FilesList":{"shared.txt":42},
                "InstalledSharedFix": {
                  "GameId":1, "Guid":"{{nestedGuid}}", "Version":1, "VersionStr":null
                }
              }
            }
            """;
        await File.WriteAllTextAsync(path, text);

        var record = Assert.IsType<FileInstalledFixEntity>(Assert.Single(await _provider.GetInstalledFixesListAsync()));

        Assert.Equal("2.3", record.Version);
        Assert.NotNull(record.InstalledSharedFix);
        Assert.Equal("4.5", record.InstalledSharedFix.Version);
        Assert.Equal(sharedGuid, record.InstalledSharedFix.Guid);
        Assert.Equal("shared-backup", record.InstalledSharedFix.BackupFolder);
        Assert.Equal(42, record.InstalledSharedFix.FilesList!["shared.txt"]);
        Assert.NotNull(record.InstalledSharedFix.InstalledSharedFix);
        Assert.Equal("1.0", record.InstalledSharedFix.InstalledSharedFix.Version);
        Assert.Equal(nestedGuid, record.InstalledSharedFix.InstalledSharedFix.Guid);
        Assert.Equal(text, await File.ReadAllTextAsync(Directory.GetFiles(Path.GetDirectoryName(path)!, "*.legacy-*").Single()));
        Assert.Empty(Directory.GetFiles(Path.GetDirectoryName(path)!, "*.corrupt-*"));
        _provider.InvalidateCache();
        var reloaded = Assert.IsType<FileInstalledFixEntity>(Assert.Single(await _provider.GetInstalledFixesListAsync()));
        Assert.Equal("4.5", reloaded.InstalledSharedFix!.Version);
        Assert.Equal("1.0", reloaded.InstalledSharedFix.InstalledSharedFix!.Version);
        Assert.Single(Directory.GetFiles(Path.GetDirectoryName(path)!, "*.legacy-*"));
    }

    [Fact]
    public async Task ConcurrentWritesAndSnapshotsDoNotLoseRecords()
    {
        _ = await _provider.GetInstalledFixesListAsync();
        await Task.WhenAll(Enumerable.Range(0, 20).Select(async index =>
        {
            await Task.Yield();
            Assert.True(_provider.CreateInstalledJson(_game, Record("1")).IsSuccess);
            _ = await _provider.GetInstalledFixesListAsync();
        }));
        Assert.Equal(20, (await _provider.GetInstalledFixesListAsync()).Count);
        Assert.Equal(20, Directory.GetFiles(Path.Combine(_root, ClientConstants.BackupFolder), "*.json").Length);
    }

    private static FileInstalledFixEntity Record(string version, Guid? guid = null) => new() { GameId = 1, Guid = guid ?? Guid.NewGuid(), Version = version };
    private string RecordPath(Guid guid) => Path.Combine(_root, ClientConstants.BackupFolder, guid + ".json");
    public void Dispose()
    {
        _provider.Dispose();
        Directory.Delete(_root, true);
    }
}
