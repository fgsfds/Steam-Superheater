using Api.Common.Interface;
using Common;
using Common.Client;
using Common.Client.FilesTools.Interfaces;
using Common.Client.Logger;
using Common.Client.Providers;
using Common.Client.Providers.Interfaces;
using Moq;

namespace Tests;

[Collection("Sync")]
public sealed class GitHubTests
{
    [Fact]
    public async Task GetFixesListFromAPI()
    {
        Mock<IGamesProvider> gamesProviderMock = new();
        Mock<IInstalledFixesProvider> installedMock = new();
        Mock<IConfigProvider> configMock = new();
        using HttpClient httpClient = new();

        ApiInterface apiInterface = new(httpClient, configMock.Object);

        FixesProvider fixesProvider = new(apiInterface, gamesProviderMock.Object, installedMock.Object, new());

        var fixes = await fixesProvider.GetFixesListAsync(false, false).ConfigureAwait(true);

        Assert.NotNull(fixes.ResultObject);

        //Looking for Alan Wake fixes list
        var result = fixes.ResultObject.Exists(static x => x.GameId == 108710);
        Assert.True(result);
    }

    [Fact]
    public async Task GetAppReleasesTest()
    {
        Mock<IFilesDownloader> filesDownloaderMock = new();
        Mock<ILogger> loggerMock = new();
        Mock<IConfigProvider> configMock = new();
        using HttpClient httpClient = new();

        ApiInterface apiInterface = new(httpClient, configMock.Object);
        AppUpdateInstaller appUpdateInstaller = new(filesDownloaderMock.Object, apiInterface, loggerMock.Object);

        var release = await appUpdateInstaller.CheckForUpdates(new("0.0.0.0")).ConfigureAwait(true);

        Assert.True(release.IsSuccess);
    }
}

