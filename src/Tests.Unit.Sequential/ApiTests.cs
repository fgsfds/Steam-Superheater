using Api.Axiom.Interfaces;
using Api.Client;
using Common.Axiom;
using Common.Client;
using Common.Client.FilesTools.Interfaces;
using Common.Client.Providers;
using Common.Client.Providers.Interfaces;
using Database.Client;
using Microsoft.Extensions.Logging;
using Moq;

namespace Tests.Unit.Sequential;

public sealed class ApiTests
{
    [Fact]
    public async Task GetFixesListFromAPI()
    {
        Mock<IGamesProvider> gamesProviderMock = new();
        Mock<IInstalledFixesProvider> installedMock = new();
        Mock<IConfigProvider> configMock = new();
        Mock<ILogger> logger = new();
        using HttpClient httpClient = CreateHttpClient();

        //IApiInterface apiInterface = new ServerApiInterface(httpClient, configMock.Object);
        IApiInterface apiInterface = new GitHubApiInterface(new(logger.Object, httpClient), httpClient, logger.Object);

        DatabaseContextFactory dbContextFactory = new();
        S3Provider s3Provider = new(apiInterface);

        FixesProvider fixesProvider = new(apiInterface, gamesProviderMock.Object, installedMock.Object, dbContextFactory, s3Provider);

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
        using HttpClient httpClient = CreateHttpClient();

        //IApiInterface apiInterface = new ServerApiInterface(httpClient, configMock.Object);
        IApiInterface apiInterface = new GitHubApiInterface(new(logger.Object, httpClient), httpClient, logger.Object);
        AppUpdateInstaller appUpdateInstaller = new(filesDownloaderMock.Object, apiInterface, loggerMock.Object);

        var release = await appUpdateInstaller.CheckForUpdates(new("0.0.0.0")).ConfigureAwait(true);

        Assert.True(release.IsSuccess);
    }

    /// <summary>
    /// Creates an HTTP client that authenticates with GitHub when a token is available so that CI runs are not
    /// throttled by the unauthenticated rate limit.
    /// </summary>
    private static HttpClient CreateHttpClient()
    {
        HttpClient httpClient = new();
        httpClient.DefaultRequestHeaders.Add("User-Agent", "Superheater");

        var token = Environment.GetEnvironmentVariable("GITHUB_TOKEN");

        if (!string.IsNullOrWhiteSpace(token))
        {
            httpClient.DefaultRequestHeaders.Authorization = new("Bearer", token);
        }

        return httpClient;
    }
}
