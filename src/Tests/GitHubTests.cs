using Api.Common.Interface;
using Common;
using Common.Client;
using Common.Client.Config;
using Common.Client.DI;
using Common.Client.FilesTools;
using Common.Client.Logger;
using Common.Client.Providers;
using Database.Client;
using Microsoft.Extensions.DependencyInjection;

namespace Tests;

[Collection("Sync")]
public sealed class GitHubTests
{
    public GitHubTests()
    {
        BindingsManager.Reset();
        var container = BindingsManager.Instance;
        _ = container.AddTransient<SteamTools>();
        _ = container.AddTransient<GamesProvider>();
        _ = container.AddTransient<InstalledFixesProvider>();
        _ = container.AddTransient<FixesProvider>();
        _ = container.AddScoped<IConfigProvider, ConfigProviderFake>();
        _ = container.AddTransient<AppUpdateInstaller>();
        _ = container.AddTransient<HttpClient>();
        _ = container.AddTransient<LoggerToFile>();
        _ = container.AddTransient<ArchiveTools>();
        _ = container.AddTransient<ProgressReport>();
        _ = container.AddTransient<ApiInterface>();
        _ = container.AddTransient<FilesDownloader>();
        _ = container.AddTransient<DatabaseContextFactory>();
    }

    [Fact]
    public async Task GetFixesListFromAPI()
    {
        var fixesProvider = BindingsManager.Provider.GetRequiredService<FixesProvider>();
        var fixes = await fixesProvider.GetFixesListAsync().ConfigureAwait(true);

        Assert.NotNull(fixes.ResultObject);

        //Looking for Alan Wake fixes list
        var result = fixes.ResultObject.Exists(static x => x.GameId == 108710);
        Assert.True(result);
    }

    [Fact]
    public async Task GetAppReleasesTest()
    {
        var fixesProvider = BindingsManager.Provider.GetRequiredService<AppUpdateInstaller>();
        var release = await fixesProvider.CheckForUpdates(new("0.0.0.0")).ConfigureAwait(true);

        Assert.True(release.IsSuccess);
    }
}

