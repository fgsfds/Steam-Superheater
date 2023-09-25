using Common.DI;
using Common.Entities;
using Common.FixTools;
using Common.Providers;
using System.IO.Compression;
using System.Security.Cryptography;

namespace Tests
{
    /// <summary>
    /// Tests that use instance data and should be run in single thread
    /// </summary>
    [TestClass]
    public class SyncTests
    {
        private const string TestTempFolder = "test_temp";
        private static readonly object LockObject = new();
        private string _currentDirectory;

        #region Test Preparations

        [TestInitialize]
        public void Init()
        {
            Monitor.Enter(LockObject);
            BindingsManager.Reset();

            _currentDirectory = Directory.GetCurrentDirectory();

            var container = BindingsManager.Instance;
            container.Options.EnableAutoVerification = false;
            container.Options.ResolveUnregisteredConcreteTypes = true;

            CommonBindings.Load(container);

            if (Directory.Exists(TestTempFolder))
            {
                Directory.Delete(TestTempFolder, true);
            }
            Directory.CreateDirectory(TestTempFolder);

            Directory.SetCurrentDirectory(Path.Combine(Directory.GetCurrentDirectory(), TestTempFolder));
        }

        [TestCleanup]
        public void Cleanup()
        {
            Directory.SetCurrentDirectory(_currentDirectory);

            Directory.Delete(TestTempFolder, true);

            if (File.Exists("test_fix.zip"))
            {
                File.Delete("test_fix.zip");
            }

            Monitor.Exit(LockObject);
        }

        #endregion Test Preparations

        #region Tests

        [TestMethod]
        public async Task GetFixesFromGithubTest()
        {
            var fixesProvider = BindingsManager.Instance.GetInstance<FixesProvider>();
            var fixes = await fixesProvider.GetNewFixesListAsync();

            //Looking for Alan Wake fixes list
            var result = fixes.Any(x => x.GameId == 108710);
        }

        [TestMethod]
        public async Task InstallUninstallFixTest() => await InstallUninstallFixAsync(null);

        [TestMethod]
        public async Task InstallUninstallFixVariantTest() => await InstallUninstallFixAsync("variant1");

        #endregion Tests

        #region Private Methods

        private static async Task InstallUninstallFixAsync(string? variant)
        {
            string fixArchive = variant is null ? "test_fix.zip" : "test_fix_variant.zip";

            string gameFolder = PrepareGameFolder();

            File.Copy($"..\\Resources\\{fixArchive}", Path.Combine(Directory.GetCurrentDirectory(), "..\\test_fix.zip"), true);

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
                FilesToDelete = "install folder\\file to delete.txt;install folder\\subfolder\\file to delete in subfolder.txt;file to delete in parent folder.txt",
                FilesToBackup = "install folder\\file to backup.txt"
            };

            var installedFix = await FixInstaller.InstallFix(gameEntity, fixEntity, variant);

            InstalledFixesProvider.SaveInstalledFixes(new List<InstalledFixEntity>() { installedFix });

            CheckNewFiles();

            fixEntity.InstalledFix = installedFix;

            //modify backed up file
            File.WriteAllText("game\\install folder\\file to backup.txt", "22");

            FixUninstaller.UninstallFix(gameEntity, fixEntity);

            CheckOriginalFiles();
        }

        private static void CheckOriginalFiles()
        {
            //check original files
            var exeExists = File.Exists("game\\install folder\\start game.exe");
            Assert.IsTrue(exeExists);

            var fi = new FileInfo("game\\install folder\\start game.exe").Length;
            Assert.IsTrue(fi == 1);

            var fileExists = File.Exists("game\\install folder\\subfolder\\file.txt");
            Assert.IsTrue(fileExists);

            var fi2 = new FileInfo("game\\install folder\\subfolder\\file.txt").Length;
            Assert.IsTrue(fi2 == 1);

            //check deleted files
            var fileToDeleteExists = File.Exists("game\\install folder\\file to delete.txt");
            Assert.IsTrue(fileToDeleteExists);

            var fileToDeleteSubExists = File.Exists("game\\install folder\\subfolder\\file to delete in subfolder.txt");
            Assert.IsTrue(fileToDeleteSubExists);

            var fileToDeleteParentExists = File.Exists("game\\file to delete in parent folder.txt");
            Assert.IsTrue(fileToDeleteParentExists);

            //check backed up files
            var fileToBackupExists = File.Exists("game\\install folder\\file to backup.txt");
            Assert.IsTrue(fileToBackupExists);

            var fi3 = new FileInfo("game\\install folder\\file to backup.txt").Length;
            Assert.IsTrue(fi3 == 1);
        }

        private static void CheckNewFiles()
        {
            //check replaced files
            var exeExists = File.Exists("game\\install folder\\start game.exe");
            Assert.IsTrue(exeExists);

            var fi = new FileInfo("game\\install folder\\start game.exe").Length;
            Assert.IsTrue(fi == 2);

            var fileExists = File.Exists("game\\install folder\\subfolder\\file.txt");
            Assert.IsTrue(fileExists);

            var fi2 = new FileInfo("game\\install folder\\subfolder\\file.txt").Length;
            Assert.IsTrue(fi2 == 2);

            //check deleted files
            var fileToDeleteExists = File.Exists("game\\install folder\\file to delete.txt");
            Assert.IsFalse(fileToDeleteExists);

            var fileToDeleteSubExists = File.Exists("game\\install folder\\subfolder\\file to delete in subfolder.txt");
            Assert.IsFalse(fileToDeleteSubExists);

            var fileToDeleteParentExists = File.Exists("game\\file to delete in parent folder.txt");
            Assert.IsFalse(fileToDeleteParentExists);

            //check backed up files
            var fileToBackupExists = File.Exists("game\\install folder\\file to backup.txt");
            Assert.IsTrue(fileToBackupExists);

            var backedUpFileExists = File.Exists("game\\.sfd\\test_fix\\install folder\\file to backup.txt");
            Assert.IsTrue(backedUpFileExists);

            //check installed.xml
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead("installed.xml"))
                {
                    var hash = Convert.ToHexString(md5.ComputeHash(stream));

                    Assert.IsTrue(hash.Equals("1ACFF09755D3D16A824E23FE1DD45B6B"));
                }
            }
        }

        private static string PrepareGameFolder()
        {
            var gameFolder = Path.Combine(Directory.GetCurrentDirectory(), "game");

            ZipFile.ExtractToDirectory("..\\Resources\\test_game.zip", gameFolder);

            return gameFolder;
        }

        #endregion Private Methods
    }
}