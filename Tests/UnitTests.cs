using SteamFDCommon.Entities;
using SteamFDCommon.Providers;
using System.Reflection;

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

        [TestMethod]
        public void GetGameEntityFromAcf()
        {
            var result = (GameEntity)typeof(GamesProvider)
                .GetMethod("GetGameEntityFromAcf", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(new GamesProvider(), new object[] { "Resources\\test_manifest.acf" });

            Assert.IsNotNull(result);

            Assert.IsTrue(result.Name.Equals("DOOM (1993)"));
            Assert.IsTrue(result.Id.Equals(2280));
            Assert.IsTrue(result.InstallDir.Equals("Resources\\common\\Ultimate Doom\\"));
            Assert.IsTrue(result.Icon.Equals("d:\\games\\[steam]\\appcache\\librarycache\\2280_icon.jpg"));

        }
    }
}