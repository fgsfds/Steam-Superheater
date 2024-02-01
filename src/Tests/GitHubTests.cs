using Common.Config;
using Common.DI;
using Common.Providers;
using Common.Providers.Cached;
using Microsoft.Extensions.DependencyInjection;

namespace Tests
{
    [Collection("Sync")]
    public sealed class GitHubTests
    {
        [Fact]
        public async Task GetFixesFromGitHubTest()
        {
            BindingsManager.Reset();
            var container = BindingsManager.Instance;
            container.AddTransient<FixesProvider>();
            container.AddTransient<ConfigProvider>();

            var fixesProvider = BindingsManager.Provider.GetRequiredService<FixesProvider>();
            var fixes = await fixesProvider.GetListAsync(false);

            //Looking for Alan Wake fixes list
            var result = fixes.Exists(static x => x.GameId == 108710);
            Assert.True(result);
        }

        [Fact]
        public async Task GetGitHubReleasesTest()
        {
            var latestRelease = await GitHubReleasesProvider.GetLatestUpdateAsync(new Version("0.0.0"));

            Assert.NotNull(latestRelease);

            var versionCompare = latestRelease.Version.CompareTo(new("0.14.1"));

            Assert.Equal(0, versionCompare);
        }
    }
}
