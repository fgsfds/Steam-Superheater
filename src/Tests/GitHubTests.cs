using Common.DI;
using Common.Providers;

namespace Tests
{
    [Collection("Sync")]
    public sealed class GitHubTests
    {
        [Fact]
        public async Task GetFixesFromGitHubTest()
        {
            var fixesProvider = BindingsManager.Instance.GetInstance<FixesProvider>();
            var fixes = await fixesProvider.GetNewListAsync();

            //Looking for Alan Wake fixes list
            var result = fixes.Any(x => x.GameId == 108710);
            Assert.True(result);
        }

        [Fact]
        public async Task GetGitHubReleasesTest()
        {
            var releases = await GitHubReleasesProvider.GetNewerReleasesListAsync(new Version("0.0.0"));
            var firstRelease = releases.Last();

            var versionActual = firstRelease.Version;
            var versionExpected = new Version("0.2.2");
            var versionCompare = versionActual.CompareTo(versionExpected);

            Assert.True(versionCompare == 0);

            var descriptionActual = firstRelease.Description;
            var descriptionExpected = "First public release";

            Assert.Equal(descriptionActual, descriptionExpected);
        }
    }
}
