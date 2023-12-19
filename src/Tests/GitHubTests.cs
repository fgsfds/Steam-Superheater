using Common.Config;
using Common.DI;
using Common.Providers;
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
            var releases = await GitHubReleasesProvider.GetNewerReleasesListAsync(new Version("0.0.0"));
            var firstRelease = releases.Last();

            var versionActual = firstRelease.Version;
            Version versionExpected = new("0.2.2");
            var versionCompare = versionActual.CompareTo(versionExpected);

            Assert.Equal(0, versionCompare);

            var descriptionActual = firstRelease.Description;

            Assert.Equal("First public release", descriptionActual);
        }
    }
}
