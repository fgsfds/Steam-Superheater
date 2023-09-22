using SteamFDCommon.Providers;

namespace Tests
{
    [TestClass]
    public class UnitTests
    {
        [TestMethod]
        public async Task GetNewerReleasesTest()
        {
            var releases = await GithubReleasesProvider.GetNewerReleasesListAsync(new Version("0.0.0"));
            var firstRelease = releases.Last();

            var versionActual = firstRelease.Version;
            var versionExpected = new Version("0.2.2");
            var versionCompare = versionActual.CompareTo(versionExpected);

            Assert.IsTrue(versionCompare == 0);

            var descriptionActual = firstRelease.Description;
            var descriptionExpected = "First public release";

            Assert.IsTrue(descriptionActual.Equals(descriptionExpected));
        }
    }
}