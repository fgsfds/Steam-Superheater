using Common;
using Common.Config;
using Common.DI;
using Common.Helpers;
using Microsoft.Extensions.DependencyInjection;
using FixesProvider = Common.Providers.FixesProvider;

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
            container.AddTransient<ConfigProvider>();
            container.AddTransient<AppUpdateInstaller>();
            container.AddTransient<HttpClient>();
            container.AddTransient<Logger>();
            container.AddTransient<ArchiveTools>();
            container.AddTransient<ProgressReport>();
        }

        [Fact]
        public async Task GetFixesListFromAPI()
        {
            var fixesProvider = BindingsManager.Provider.GetRequiredService<FixesProvider>();
            var fixes = await fixesProvider.GetFixesListAsync().ConfigureAwait(true);

            //Looking for Alan Wake fixes list
            var result = fixes.Exists(static x => x.GameId == 108710);
            Assert.True(result);
        }

        [Fact]
        public async Task GetAppReleasesTest()
        {
            var fixesProvider = BindingsManager.Provider.GetRequiredService<AppUpdateInstaller>();
            var release = await fixesProvider.CheckForUpdates(new("0.0.0.0")).ConfigureAwait(true);

            Assert.True(release);
        }
    }
}
