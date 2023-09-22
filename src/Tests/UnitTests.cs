using SteamFDCommon.DI;
using SteamFDCommon.Entities;
using SteamFDCommon.FixTools;
using SteamFDCommon.Providers;
using System.IO.Compression;
using System.Reflection;
using System.Security.Cryptography;

namespace Tests
{
    [TestClass]
    public class UnitTests
    {
        private const string TestTempFolder = "test_temp";

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

        [TestMethod]
        public async Task InstallUninstallFix()
        {
            var currentDirectory = Directory.GetCurrentDirectory();

            try
            {
                string gameFolder = PrepareGameFolderAndSetWorkingDirectory();

                GameEntity gameEntity = new(
                    1,
                    "test game",
                    gameFolder
                );

                FixEntity fixEntity = new()
                {
                    Name = "test fix",
                    Version = 1,
                    Guid = Guid.Parse("C0650F19-F670-4F8A-8545-70F6C5171FA5"),
                    Url = "test_fix.zip",
                    InstallFolder = "install folder",
                    FilesToDelete = "install folder\\file to delete.txt"
                };

                var installedFix = await FixInstaller.InstallFix(gameEntity, fixEntity, null);

                InstalledFixesProvider.SaveInstalledFixes(new List<InstalledFixEntity>() { installedFix });

                CheckNewFiles();

                fixEntity.InstalledFix = installedFix;

                FixUninstaller.UninstallFix(gameEntity, fixEntity);

                CheckOriginalFiles();
            }
            finally
            {
                Directory.SetCurrentDirectory(currentDirectory);
                Directory.Delete(TestTempFolder, true);

                if (File.Exists("test_fix.zip"))
                {
                    File.Delete("test_fix.zip");
                }
            }
        }

        private static void CheckOriginalFiles()
        {
            var exeExists = File.Exists("game\\install folder\\start game.exe");
            Assert.IsTrue(exeExists);

            var fi = new FileInfo("game\\install folder\\start game.exe").Length;
            Assert.IsTrue(fi == 1);

            var fileExists = File.Exists("game\\install folder\\subfolder\\file.txt");
            Assert.IsTrue(fileExists);

            var fi2 = new FileInfo("game\\install folder\\subfolder\\file.txt").Length;
            Assert.IsTrue(fi2 == 1);

            var fileToDeleteExists = File.Exists("game\\install folder\\file to delete.txt");
            Assert.IsTrue(fileToDeleteExists);
        }

        private static void CheckNewFiles()
        {
            var exeExists = File.Exists("game\\install folder\\start game.exe");
            Assert.IsTrue(exeExists);

            var fi = new FileInfo("game\\install folder\\start game.exe").Length;
            Assert.IsTrue(fi == 2);

            var fileExists = File.Exists("game\\install folder\\subfolder\\file.txt");
            Assert.IsTrue(fileExists);

            var fi2 = new FileInfo("game\\install folder\\subfolder\\file.txt").Length;
            Assert.IsTrue(fi2 == 2);

            var fileToDeleteExists = File.Exists("game\\install folder\\file to delete.txt");
            Assert.IsFalse(fileToDeleteExists);

            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead("installed.xml"))
                {
                    var hash = Convert.ToHexString(md5.ComputeHash(stream));

                    Assert.IsTrue(hash.Equals("1ACFF09755D3D16A824E23FE1DD45B6B"));
                }
            }
        }

        private static string PrepareGameFolderAndSetWorkingDirectory()
        {
            if (Directory.Exists(TestTempFolder))
            {
                Directory.Delete(TestTempFolder, true);
            }
            Directory.CreateDirectory(TestTempFolder);

            File.Copy("Resources\\test_fix.zip", Path.Combine(Directory.GetCurrentDirectory(), "test_fix.zip"), true);

            Directory.SetCurrentDirectory(Path.Combine(Directory.GetCurrentDirectory(), TestTempFolder));

            var gameFolder = Path.Combine(Directory.GetCurrentDirectory(), "game");

            ZipFile.ExtractToDirectory("..\\Resources\\test_game.zip", gameFolder);

            return gameFolder;
        }
    }
}