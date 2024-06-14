using Common.Client;
using Common.Client.API;
using Common.Client.Config;
using Common.Client.DI;
using Common.Client.Providers;
using Microsoft.Extensions.DependencyInjection;

namespace Tests
{
    [Collection("Sync")]
    public sealed class GitHubTests
    {
        public GitHubTests()
        {
            BindingsManager.Reset();
            var container = BindingsManager.Instance;
            container.AddTransient<FixesProvider>();
            container.AddScoped<IConfigProvider, ConfigProviderFake>();
            container.AddTransient<AppUpdateInstaller>();
            container.AddTransient<HttpClient>();
            container.AddTransient<Logger>();
            container.AddTransient<ArchiveTools>();
            container.AddTransient<ProgressReport>();
            container.AddTransient<ApiInterface>();
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
}
