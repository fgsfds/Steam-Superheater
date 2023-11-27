using Common.Config;
using Common.DI;
using Common.Entities;
using Common.Entities.Fixes;
using Common.Entities.Fixes.FileFix;
using Common.FixTools;
using Common.Helpers;
using Common.Providers;
using Microsoft.Extensions.DependencyInjection;
using System.IO.Compression;
using System.Security.Cryptography;

namespace Tests
{
    /// <summary>
    /// Tests that use instance data and should be run in a single thread
    /// </summary>
    [Collection("Sync")]
    public sealed class FileFixTests : IDisposable
    {
        private const string TestTempFolder = "test_temp";
        private readonly string _currentDirectory;

        private readonly FixManager _fixManager;

        #region Test Preparations

        public FileFixTests()
        {
            BindingsManager.Reset();
            var container = BindingsManager.Instance;
            container.AddScoped<ConfigProvider>();
            CommonBindings.Load(container);

            _currentDirectory = Directory.GetCurrentDirectory();

            _fixManager = BindingsManager.Provider.GetRequiredService<FixManager>();

            if (Directory.Exists(TestTempFolder))
            {
                Directory.Delete(TestTempFolder, true);
            }

            Directory.CreateDirectory(TestTempFolder);

            Directory.SetCurrentDirectory(Path.Combine(Directory.GetCurrentDirectory(), TestTempFolder));
        }

        public void Dispose()
        {
            Directory.SetCurrentDirectory(_currentDirectory);

            Directory.Delete(TestTempFolder, true);

            if (File.Exists("test_fix.zip"))
            {
                File.Delete("test_fix.zip");
            }
        }

        #endregion Test Preparations

        #region Tests

        [Fact]
        public Task InstallUninstallFixTest() => InstallUninstallFixAsync(variant: null, update: false);

        [Fact]
        public Task InstallUninstallFixVariantTest() => InstallUninstallFixAsync(variant: "variant2", update: false);

        [Fact]
        public Task UpdateFixTest() => InstallUninstallFixAsync(variant: null, update: true);

        [Fact]
        public async Task InstallCompromisedFixTest()
        {
            GameEntity gameEntity = new()
            {
                Id = 1,
                Name = "test game",
                InstallDir = "game folder"
            };

            FileFixEntity fixEntity = new()
            {
                Name = "test fix",
                Version = 1,
                Guid = Guid.Parse("C0650F19-F670-4F8A-8545-70F6C5171FA5"),
                Url = "https://github.com/fgsfds/SteamFD-Fixes-Repo/raw/master/fixes/bsp_nointro_v1.zip",
                MD5 = "badMD5"
            };

            try
            {
                await _fixManager.InstallFixAsync(gameEntity, fixEntity, null, false);
            }
            catch (HashCheckFailedException)
            {
                //method failed successfully
                return;
            }

            Assert.Fail();
        }

        [Fact]
        public async Task InstallFixToANewFolder()
        {
            var gameFolder = PrepareGameFolder();

            File.Copy("..\\Resources\\test_fix.zip", Path.Combine(Directory.GetCurrentDirectory(), "..\\test_fix.zip"), true);

            GameEntity gameEntity = new()
            {
                Id = 1,
                Name = "test game",
                InstallDir = gameFolder
            };

            FileFixEntity fixEntity = new()
            {
                Name = "test fix",
                Version = 1,
                Guid = Guid.Parse("C0650F19-F670-4F8A-8545-70F6C5171FA5"),
                Url = "test_fix.zip",
                InstallFolder = "new folder"
            };

            var installedFix = await _fixManager.InstallFixAsync(gameEntity, fixEntity, null, true);

            InstalledFixesProvider.SaveInstalledFixes([installedFix]);

            var exeExists = File.Exists("game\\new folder\\start game.exe");
            Assert.True(exeExists);

            fixEntity.InstalledFix = installedFix;

            _fixManager.UninstallFix(gameEntity, fixEntity);

            var newDirExists = Directory.Exists("game\\new folder");
            Assert.False(newDirExists);
        }

        #endregion Tests

        #region Private Methods

        private async Task InstallUninstallFixAsync(string? variant, bool update)
        {
            var fixArchive = variant is null ? "test_fix.zip" : "test_fix_variant.zip";

            var fixArchiveMD5 = variant is null ? "4E9DE15FC40592B26421E05882C2F6F7" : "DA2D7701D2EB5BC9A35FB58B3B04C5B9";

            var gameFolder = PrepareGameFolder();

            File.Copy($"..\\Resources\\{fixArchive}", Path.Combine(Directory.GetCurrentDirectory(), "..\\test_fix.zip"), true);

            GameEntity gameEntity = new()
            {
                Id = 1,
                Name = "test game",
                InstallDir = gameFolder
            };

            FileFixEntity fixEntity = new()
            {
                Name = "test fix",
                Version = 1,
                Guid = Guid.Parse("C0650F19-F670-4F8A-8545-70F6C5171FA5"),
                Url = "test_fix.zip",
                InstallFolder = "install folder",
                FilesToDelete = ["install folder\\file to delete.txt", "install folder\\subfolder\\file to delete in subfolder.txt", "file to delete in parent folder.txt"],
                FilesToBackup = ["install folder\\file to backup.txt"],
                MD5 = fixArchiveMD5
            };

            var installedFix = await _fixManager.InstallFixAsync(gameEntity, fixEntity, variant, true);

            InstalledFixesProvider.SaveInstalledFixes([installedFix]);

            CheckNewFiles();

            fixEntity.InstalledFix = installedFix;

            //modify backed up file
            await File.WriteAllTextAsync("game\\install folder\\file to backup.txt", "modified");

            if (update)
            {
                fixEntity.InstalledFix = await UpdateFixAsync(gameEntity, fixEntity.InstalledFix);
            }

            _fixManager.UninstallFix(gameEntity, fixEntity);

            CheckOriginalFiles();
        }

        private async Task<BaseInstalledFixEntity> UpdateFixAsync(GameEntity gameEntity, BaseInstalledFixEntity installedFix)
        {
            File.Copy("..\\Resources\\test_fix_v2.zip", Path.Combine(Directory.GetCurrentDirectory(), "..\\test_fix_v2.zip"), true);

            FileFixEntity fixEntity = new()
            {
                Name = "test fix",
                Version = 2,
                Guid = Guid.Parse("C0650F19-F670-4F8A-8545-70F6C5171FA5"),
                Url = "test_fix_v2.zip",
                InstallFolder = "install folder",
                FilesToDelete = ["install folder\\file to delete.txt", "install folder\\subfolder\\file to delete in subfolder.txt", "file to delete in parent folder.txt"],
                FilesToBackup = ["install folder\\file to backup.txt"],
                InstalledFix = installedFix
            };

            var newInstalledFix = await _fixManager.UpdateFixAsync(gameEntity, fixEntity, null, true);

            InstalledFixesProvider.SaveInstalledFixes([newInstalledFix]);

            CheckUpdatedFiles();

            return newInstalledFix;
        }

        private static void CheckOriginalFiles()
        {
            //check original files
            var exeExists = File.Exists("game\\install folder\\start game.exe");
            Assert.True(exeExists);

            var fi = File.ReadAllTextAsync("game\\install folder\\start game.exe").Result;
            Assert.Equal("original", fi);

            var fileExists = File.Exists("game\\install folder\\subfolder\\file.txt");
            Assert.True(fileExists);

            var fi2 = File.ReadAllTextAsync("game\\install folder\\subfolder\\file.txt").Result;
            Assert.Equal("original", fi2);

            //check deleted files
            var fileToDeleteExists = File.Exists("game\\install folder\\file to delete.txt");
            Assert.True(fileToDeleteExists);

            var fileToDeleteSubExists = File.Exists("game\\install folder\\subfolder\\file to delete in subfolder.txt");
            Assert.True(fileToDeleteSubExists);

            var fileToDeleteParentExists = File.Exists("game\\file to delete in parent folder.txt");
            Assert.True(fileToDeleteParentExists);

            //check backed up files
            var fileToBackupExists = File.Exists("game\\install folder\\file to backup.txt");
            Assert.True(fileToBackupExists);

            var fi3 = File.ReadAllTextAsync("game\\install folder\\file to backup.txt").Result;
            Assert.Equal("original", fi3);
        }

        private static void CheckNewFiles()
        {
            //check replaced files
            var exeExists = File.Exists("game\\install folder\\start game.exe");
            Assert.True(exeExists);

            var fi = File.ReadAllTextAsync("game\\install folder\\start game.exe").Result;
            Assert.Equal("fix_v1", fi);

            var fileExists = File.Exists("game\\install folder\\subfolder\\file.txt");
            Assert.True(fileExists);

            var fi2 = File.ReadAllTextAsync("game\\install folder\\subfolder\\file.txt").Result;
            Assert.Equal("fix_v1", fi2);

            //check deleted files
            var fileToDeleteExists = File.Exists("game\\install folder\\file to delete.txt");
            Assert.False(fileToDeleteExists);

            var fileToDeleteSubExists = File.Exists("game\\install folder\\subfolder\\file to delete in subfolder.txt");
            Assert.False(fileToDeleteSubExists);

            var fileToDeleteParentExists = File.Exists("game\\file to delete in parent folder.txt");
            Assert.False(fileToDeleteParentExists);

            //check backed up files
            var fileToBackupExists = File.Exists("game\\install folder\\file to backup.txt");
            Assert.True(fileToBackupExists);

            var backedUpFileExists = File.Exists("game\\.sfd\\test_fix\\install folder\\file to backup.txt");
            Assert.True(backedUpFileExists);

            //check installed.xml
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(Consts.InstalledFile))
                {
                    var hash = Convert.ToHexString(md5.ComputeHash(stream));

                    Assert.Equal("E529E785414A1805C467C6407E4A8FC4", hash);
                }
            }
        }

        private static void CheckUpdatedFiles()
        {
            //check replaced files
            var exeExists = File.Exists("game\\install folder\\start game.exe");
            Assert.True(exeExists);

            var fi = File.ReadAllTextAsync("game\\install folder\\start game.exe").Result;
            Assert.Equal("fix_v2", fi);

            var fileExists = File.Exists("game\\install folder\\subfolder\\file.txt");
            Assert.True(fileExists);

            var fi2 = File.ReadAllTextAsync("game\\install folder\\subfolder\\file.txt").Result;
            Assert.Equal("original", fi2);

            //check deleted files
            var fileToDeleteExists = File.Exists("game\\install folder\\file to delete.txt");
            Assert.False(fileToDeleteExists);

            var fileToDeleteSubExists = File.Exists("game\\install folder\\subfolder\\file to delete in subfolder.txt");
            Assert.False(fileToDeleteSubExists);

            var fileToDeleteParentExists = File.Exists("game\\file to delete in parent folder.txt");
            Assert.False(fileToDeleteParentExists);

            //check backed up files
            var fileToBackupExists = File.Exists("game\\install folder\\file to backup.txt");
            Assert.True(fileToBackupExists);

            var backedUpFileExists = File.Exists("game\\.sfd\\test_fix\\install folder\\file to backup.txt");
            Assert.True(backedUpFileExists);
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