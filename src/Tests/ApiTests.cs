using Api.Common.Interface;
using Api.Common.Interface.ServerApiInterface;
using Common;
using Common.Client;
using Common.Client.FilesTools.Interfaces;
using Common.Client.Providers;
using Common.Client.Providers.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace Tests;

[Collection("Sync")]
public sealed class ApiTests
{
    [Fact]
    public async Task GetFixesListFromAPI()
    {
        Mock<IGamesProvider> gamesProviderMock = new();
        Mock<IInstalledFixesProvider> installedMock = new();
        Mock<IConfigProvider> configMock = new();
        Mock<ILogger> logger = new();
        using HttpClient httpClient = new();
        httpClient.DefaultRequestHeaders.Add("User-Agent", "Superheater");

        //IApiInterface apiInterface = new ServerApiInterface(httpClient, configMock.Object);
        IApiInterface apiInterface = new FileApiInterface(new(logger.Object, httpClient), httpClient);
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
        Mock<ILogger> logger = new();
        using HttpClient httpClient = new();
        httpClient.DefaultRequestHeaders.Add("User-Agent", "Superheater");

        //IApiInterface apiInterface = new ServerApiInterface(httpClient, configMock.Object);
        IApiInterface apiInterface = new FileApiInterface(new(logger.Object, httpClient), httpClient);
        AppUpdateInstaller appUpdateInstaller = new(filesDownloaderMock.Object, apiInterface, loggerMock.Object);

        var release = await appUpdateInstaller.CheckForUpdates(new("0.0.0.0")).ConfigureAwait(true);

        Assert.True(release.IsSuccess);
    }
}

