using Api.Axiom.Interfaces;
using Common.Axiom;
using Common.Axiom.Entities;
using Common.Axiom.Enums;
using Common.Client;
using Common.Client.FilesTools.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace Tests.Unit;

/// <summary>
/// Tests for <see cref="AppUpdateInstaller"/>
/// </summary>
public sealed class AppUpdateInstallerTests
{
    /// <summary>
    /// A failed download is returned and the archive is not unpacked
    /// </summary>
    [Fact]
    public async Task DownloadAndUnpackReturnsErrorWhenDownloadFails()
    {
        var release = new AppReleaseEntity()
        {
            Version = new Version(999, 0),
            Description = "test release",
            DownloadUrl = new Uri("https://example.com/superheater_update_test_package.zip"),
        };

        var apiMock = new Mock<IApiInterface>();
        _ = apiMock
            .Setup(x => x.GetLatestAppReleaseAsync(It.IsAny<OSEnum>()))
            .ReturnsAsync(new Result<AppReleaseEntity?>(ResultEnum.Success, release, string.Empty));

        var downloaderMock = new Mock<IFilesDownloader>();
        _ = downloaderMock
            .Setup(x => x.DownloadFileAsync(It.IsAny<Uri>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Result(ResultEnum.ConnectionError, "download failed"));

        AppUpdateInstaller installer = new(downloaderMock.Object, apiMock.Object, new Mock<ILogger>().Object);

        var checkResult = await installer.CheckForUpdates(new Version(1, 0)).ConfigureAwait(true);
        Assert.True(checkResult.IsSuccess);

        var result = await installer.DownloadAndUnpackLatestRelease(CancellationToken.None).ConfigureAwait(true);

        Assert.Equal(ResultEnum.ConnectionError, result.ResultEnum);
        downloaderMock.Verify(x => x.DownloadFileAsync(It.IsAny<Uri>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// An API failure is returned as-is instead of being treated as no new release
    /// </summary>
    [Fact]
    public async Task CheckForUpdatesReturnsErrorWhenApiFails()
    {
        var apiMock = new Mock<IApiInterface>();
        _ = apiMock
            .Setup(x => x.GetLatestAppReleaseAsync(It.IsAny<OSEnum>()))
            .ReturnsAsync(new Result<AppReleaseEntity?>(ResultEnum.ConnectionError, null, "GitHub is not responding"));

        AppUpdateInstaller installer = new(new Mock<IFilesDownloader>().Object, apiMock.Object, new Mock<ILogger>().Object);

        var result = await installer.CheckForUpdates(new Version(1, 0)).ConfigureAwait(true);

        Assert.Equal(ResultEnum.ConnectionError, result.ResultEnum);
        Assert.Equal("GitHub is not responding", result.Message);
    }

    /// <summary>
    /// A not found API result is propagated instead of being treated as no new release
    /// </summary>
    [Fact]
    public async Task CheckForUpdatesReturnsNotFoundWhenApiReturnsNotFound()
    {
        var apiMock = new Mock<IApiInterface>();
        _ = apiMock
            .Setup(x => x.GetLatestAppReleaseAsync(It.IsAny<OSEnum>()))
            .ReturnsAsync(new Result<AppReleaseEntity?>(ResultEnum.NotFound, null, "No release found for Windows"));

        AppUpdateInstaller installer = new(new Mock<IFilesDownloader>().Object, apiMock.Object, new Mock<ILogger>().Object);

        var result = await installer.CheckForUpdates(new Version(1, 0)).ConfigureAwait(true);

        Assert.Equal(ResultEnum.NotFound, result.ResultEnum);
    }

    /// <summary>
    /// A release that is not newer than the current version is reported as not found
    /// </summary>
    [Fact]
    public async Task CheckForUpdatesReturnsNotFoundWhenNoNewerRelease()
    {
        var release = new AppReleaseEntity()
        {
            Version = new Version(1, 0),
            Description = "test release",
            DownloadUrl = new Uri("https://example.com/win-x64.zip"),
        };

        var apiMock = new Mock<IApiInterface>();
        _ = apiMock
            .Setup(x => x.GetLatestAppReleaseAsync(It.IsAny<OSEnum>()))
            .ReturnsAsync(new Result<AppReleaseEntity?>(ResultEnum.Success, release, string.Empty));

        AppUpdateInstaller installer = new(new Mock<IFilesDownloader>().Object, apiMock.Object, new Mock<ILogger>().Object);

        var result = await installer.CheckForUpdates(new Version(2, 0)).ConfigureAwait(true);

        Assert.Equal(ResultEnum.NotFound, result.ResultEnum);
    }

    /// <summary>
    /// A release newer than the current version is reported as a successful update
    /// </summary>
    [Fact]
    public async Task CheckForUpdatesReturnsSuccessWhenNewerRelease()
    {
        var release = new AppReleaseEntity()
        {
            Version = new Version(2, 0),
            Description = "test release",
            DownloadUrl = new Uri("https://example.com/win-x64.zip"),
        };

        var apiMock = new Mock<IApiInterface>();
        _ = apiMock
            .Setup(x => x.GetLatestAppReleaseAsync(It.IsAny<OSEnum>()))
            .ReturnsAsync(new Result<AppReleaseEntity?>(ResultEnum.Success, release, string.Empty));

        AppUpdateInstaller installer = new(new Mock<IFilesDownloader>().Object, apiMock.Object, new Mock<ILogger>().Object);

        var result = await installer.CheckForUpdates(new Version(1, 0)).ConfigureAwait(true);

        Assert.Equal(ResultEnum.Success, result.ResultEnum);
    }
}
