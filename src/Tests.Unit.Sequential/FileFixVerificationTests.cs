using System.IO.Compression;
using System.Security.Cryptography;
using Api.Axiom.Interfaces;
using Common.Axiom;
using Common.Axiom.Entities;
using Common.Axiom.Entities.Fixes.FileFix;
using Common.Client;
using Common.Client.FilesTools;
using Common.Client.FilesTools.Interfaces;
using Common.Client.FixTools.FileFix;
using Common.Client.Providers.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace Tests.Unit.Sequential;

public sealed class FileFixVerificationTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "superheater-verification-" + Guid.NewGuid().ToString("N"));
    private readonly ProgressReport _progress = new();
    private readonly Mock<IFilesDownloader> _downloader = new(MockBehavior.Strict);
    private readonly FileFixInstaller _installer;
    private readonly GameEntity _game;
    private readonly string _archive;
    private readonly string _hash;

    public FileFixVerificationTests()
    {
        _ = Directory.CreateDirectory(_root);
        _ = Directory.CreateDirectory(Path.Combine(_root, "fixes"));
        _archive = Path.Combine(_root, "source.zip");
        using (var zip = ZipFile.Open(_archive, ZipArchiveMode.Create))
        {
            using var writer = new StreamWriter(zip.CreateEntry("installed.txt").Open());
            writer.Write("payload");
        }

        _hash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(_archive)));
        var config = new Mock<IConfigProvider>();
        _ = config.SetupGet(x => x.UseLocalApiAndRepo).Returns(true);
        _ = config.SetupGet(x => x.LocalRepoPath).Returns(_root);
        _installer = new FileFixInstaller(config.Object, new ArchiveTools(_progress), _downloader.Object,
            _progress, new Mock<ILogger>().Object, new Mock<IApiInterface>().Object, new Mock<IFixesProvider>().Object);
        _game = new GameEntity
        {
            Id = 1, Name = "game", InstallDir = Path.Combine(_root, "game"), Icon = string.Empty, BuildId = 1, TargetBuildId = 1
        };
        _ = Directory.CreateDirectory(_game.InstallDir);
        File.WriteAllText(Path.Combine(_game.InstallDir, "original.txt"), "original");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("invalid")]
    [InlineData("0000000000000000000000000000000000000000000000000000000000000000")]
    public async Task InvalidOrMismatchedLocalHashDoesNotModifyGame(string? hash)
    {
        var fix = CreateFix(new Uri(_archive).AbsoluteUri, hash);
        var result = await _installer.InstallFixAsync(_game, fix, null, false, CancellationToken.None);
        Assert.Equal(ResultEnum.HashError, result.ResultEnum);
        Assert.Contains("SHA-256", result.Message);
        AssertOriginalUnchanged();
        Assert.True(File.Exists(_archive));
        Assert.Equal(string.Empty, _progress.OperationMessage);
    }

    [Fact]
    public async Task MatchingLocalHashIsVerifiedAndInstalled()
    {
        var result = await _installer.InstallFixAsync(_game, CreateFix(new Uri(_archive).AbsoluteUri, _hash.ToLowerInvariant()), null, false, CancellationToken.None);
        Assert.True(result.IsSuccess, result.Message);
        Assert.True(File.Exists(Path.Combine(_game.InstallDir, "installed.txt")));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExplicitBypassAcceptsCachedArchiveWithoutHash(bool localUri)
    {
        var cached = Path.Combine(_root, "fixes", "cached.zip");
        File.Copy(_archive, cached);
        var url = localUri ? new Uri(_archive).AbsoluteUri : "https://example.test/cached.zip";
        var result = await _installer.InstallFixAsync(_game, CreateFix(url, null), null, true, CancellationToken.None);
        Assert.True(result.IsSuccess, result.Message);
        Assert.True(File.Exists(cached));
        _downloader.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task PreCancelledInstallationDoesNotMoveOriginals()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        var result = await _installer.InstallFixAsync(_game, CreateFix(new Uri(_archive).AbsoluteUri, _hash), null, false, cancellation.Token);
        Assert.Equal(ResultEnum.Cancelled, result.ResultEnum);
        AssertOriginalUnchanged();
    }

    [Fact]
    public async Task CancellationAfterVerificationStartsDoesNotSucceed()
    {
        using var cancellation = new CancellationTokenSource();
        _progress.NotifyOperationMessageChanged += message =>
        {
            if (message.StartsWith("Checking", StringComparison.Ordinal))
            {
                cancellation.Cancel();
            }
        };
        var result = await _installer.InstallFixAsync(_game, CreateFix(new Uri(_archive).AbsoluteUri, _hash), null, false, cancellation.Token);
        Assert.Equal(ResultEnum.Cancelled, result.ResultEnum);
        AssertOriginalUnchanged();
        Assert.Equal(string.Empty, _progress.OperationMessage);
    }

    [Fact]
    public async Task DownloadedMismatchReturnsHashErrorInsteadOfDownloadMessage()
    {
        var url = new Uri("https://example.test/download.zip");
        _ = _downloader.Setup(x => x.DownloadFileAsync(url, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns((Uri _, string target, CancellationToken _) =>
            {
                File.Copy(_archive, target);
                return Task.FromResult(new Result(ResultEnum.Success, "download succeeded"));
            });
        var result = await _installer.InstallFixAsync(_game, CreateFix(url.AbsoluteUri, new string('0', 64)), null, false, CancellationToken.None);
        Assert.Equal(ResultEnum.HashError, result.ResultEnum);
        Assert.Contains("SHA-256", result.Message);
        AssertOriginalUnchanged();
    }

    [Fact]
    public async Task CancelledCachedVerificationPreservesCachedFile()
    {
        var cached = Path.Combine(_root, "fixes", "cached.zip");
        File.Copy(_archive, cached);
        using var cancellation = new CancellationTokenSource();
        _progress.NotifyOperationMessageChanged += message =>
        {
            if (message.StartsWith("Checking", StringComparison.Ordinal))
            {
                cancellation.Cancel();
            }
        };
        var result = await _installer.InstallFixAsync(_game, CreateFix("https://example.test/cached.zip", _hash), null, false, cancellation.Token);
        Assert.Equal(ResultEnum.Cancelled, result.ResultEnum);
        Assert.True(File.Exists(cached));
        AssertOriginalUnchanged();
        _downloader.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task MissingFileReturnsErrorAndClearsProgress()
    {
        var result = await _installer.CheckFileHashAsync(Path.Combine(_root, "missing"), _hash, CancellationToken.None);
        Assert.Equal(ResultEnum.Error, result.ResultEnum);
        Assert.Equal(string.Empty, _progress.OperationMessage);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FailedUpdateVerificationKeepsInstalledFiles(bool cancelled)
    {
        var fix = CreateFix(new Uri(_archive).AbsoluteUri, new string('0', 64));
        fix.InstalledFix = new FileInstalledFixEntity
        {
            GameId = _game.Id, Guid = fix.Guid, Version = "1.0", FilesList = new() { ["original.txt"] = 0 }
        };
        using var cancellation = new CancellationTokenSource();
        if (cancelled)
        {
            await cancellation.CancelAsync();
        }

        var updater = new FileFixUpdater(_installer, new FileFixUninstaller());
        var result = await updater.UpdateFixAsync(_game, fix, null, false, cancellation.Token);
        Assert.Equal(cancelled ? ResultEnum.Cancelled : ResultEnum.HashError, result.ResultEnum);
        AssertOriginalUnchanged();
    }

    [Fact]
    public async Task PreCancelledHashCheckReturnsCancelledEvenForMissingFile()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        var result = await _installer.CheckFileHashAsync(Path.Combine(_root, "missing"), _hash, cancellation.Token);
        Assert.Equal(ResultEnum.Cancelled, result.ResultEnum);
        Assert.Equal(string.Empty, _progress.OperationMessage);
    }
    private static FileFixEntity CreateFix(string url, string? hash)
    {
        var fix = FileFixEntity.CreateBlank();
        fix.Name = "verification test";
        fix.Url = url;
        fix.Sha256 = hash;
        fix.FilesToDelete = ["original.txt"];
        return fix;
    }

    private void AssertOriginalUnchanged()
    {
        Assert.Equal("original", File.ReadAllText(Path.Combine(_game.InstallDir, "original.txt")));
        Assert.False(File.Exists(Path.Combine(_game.InstallDir, "installed.txt")));
        Assert.False(Directory.Exists(Path.Combine(_game.InstallDir, ClientConstants.BackupFolder)));
    }

    public void Dispose()
    {
        Directory.Delete(_root, true);
    }
}
