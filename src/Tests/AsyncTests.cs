using Common.Entities;
using Common.Providers;
using System.Reflection;

namespace Tests
{
    /// <summary>
    /// Tests that can be run in parallel
    /// </summary>
    [TestClass]
    public sealed class AsyncTests
    {
        [TestMethod]
        public async Task GetNewerReleasesTest()
        {
            var releases = await GitHubReleasesProvider.GetNewerReleasesListAsync(new Version("0.0.0"));
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
        public void GetGameEntityFromAcfTest()
        {
            var method = typeof(GamesProvider).GetMethod("GetGameEntityFromAcf", BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.IsNotNull(method);

            var result = method.Invoke(new GamesProvider(), new object[] { "Resources\\test_manifest.acf" });

            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(GameEntity));

            var gameEntity = (GameEntity)result;

            Assert.IsTrue(gameEntity.Name.Equals("DOOM (1993)"));
            Assert.IsTrue(gameEntity.Id.Equals(2280));
            Assert.IsTrue(gameEntity.InstallDir.Equals("Resources\\common\\Ultimate Doom\\"));
        }
    }
}