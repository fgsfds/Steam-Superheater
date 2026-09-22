using Api.Axiom.Interfaces;
using Common.Axiom;
using Common.Axiom.Entities;
using Common.Axiom.Enums;
using Common.Client;
using Common.Client.FilesTools.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace Tests;

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
}
