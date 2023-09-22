using SteamFDCommon.DI;
using SteamFDCommon.Entities;
using SteamFDCommon.Providers;
using System.Reflection;

namespace Tests
{
    [TestClass]
    public class UnitTests
    {
        static UnitTests()
        {
            var container = BindingsManager.Instance;
            container.Options.EnableAutoVerification = false;
            container.Options.ResolveUnregisteredConcreteTypes = true;

            CommonBindings.Load(container);
        }

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
            var method = typeof(GamesProvider).GetMethod("GetGameEntityFromAcf", BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.IsNotNull(method);

            var result = method.Invoke(new GamesProvider(), new object[] { "Resources\\test_manifest.acf" });

            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(GameEntity));

            var gameEntity = (GameEntity)result;

            Assert.IsTrue(gameEntity.Name.Equals("DOOM (1993)"));
            Assert.IsTrue(gameEntity.Id.Equals(2280));
            Assert.IsTrue(gameEntity.InstallDir.Equals("Resources\\common\\Ultimate Doom\\"));
            Assert.IsTrue(gameEntity.Icon.Equals("d:\\games\\[steam]\\appcache\\librarycache\\2280_icon.jpg"));
        }

        [TestMethod]
        public async Task GetFixesFromGithubAsync()
        {
            var fixesProvider = BindingsManager.Instance.GetInstance<FixesProvider>();
            var fixes = await fixesProvider.GetNewFixesListAsync();

            var result = fixes.Any(x => x.GameId == 108710);
        }
    }
}