using Common.Config;
using Common.DI;
using Common.Entities;
using Common.Entities.Fixes.FileFix;
using Common.FixTools;
using Common.Helpers;
using Common.Providers;
using Microsoft.Extensions.DependencyInjection;
using System.IO.Compression;

namespace Tests
{
    /// <summary>
    /// Tests that use instance data and should be run in a single thread
    /// </summary>
    [Collection("Sync")]
    public sealed class FileFixTests : IDisposable
    {
        private const string TestTempFolder = "test_temp";
        private const string TestFixZip = "test_fix.zip";
        private const string TestFixV2Zip = "test_fix_v2.zip";
        private const string TestFixVariantZip = "test_fix_variant.zip";
        private const string TestFixPatchZip = "test_fix_patch.zip";

        private readonly string _rootDirectory;
        private readonly string _testDirectory;

        private readonly GameEntity _gameEntity;
        private readonly FileFixEntity _fileFixEntity;

        private readonly FixManager _fixManager;
        private readonly InstalledFixesProvider _installedFixesProvider;

        #region Test Preparations

        public FileFixTests()
        {
            BindingsManager.Reset();
            var container = BindingsManager.Instance;
            container.AddScoped<ConfigProvider>();
            container.AddScoped<InstalledFixesProvider>();
            container.AddScoped<FixesProvider>();
            CommonBindings.Load(container);

            _rootDirectory = Directory.GetCurrentDirectory();
            _testDirectory = Path.Combine(_rootDirectory, TestTempFolder);

            if (Directory.Exists(TestTempFolder))
            {
                Directory.Delete(TestTempFolder, true);
            }

            Directory.CreateDirectory(TestTempFolder);
            Directory.SetCurrentDirectory(_testDirectory);

            _gameEntity = new()
            {
                Id = 1,
                Name = "test game",
                InstallDir = PrepareGameFolder()
            };

            _fileFixEntity = new()
            {
                Name = "test fix",
                Version = 1,
                Guid = Guid.Parse("C0650F19-F670-4F8A-8545-70F6C5171FA5"),
                Url = TestFixZip,
                InstallFolder = "install folder",
                FilesToDelete = ["install folder\\file to delete.txt", "install folder\\subfolder\\file to delete in subfolder.txt", "file to delete in parent folder.txt"],
                FilesToBackup = ["install folder\\file to backup.txt"],
                MD5 = "4E9DE15FC40592B26421E05882C2F6F7"
            };

            _fixManager = BindingsManager.Provider.GetRequiredService<FixManager>();
            _installedFixesProvider = BindingsManager.Provider.GetRequiredService<InstalledFixesProvider>();

            //create cache;
            _ = _installedFixesProvider.GetListAsync(false).Result;
        }

        public void Dispose()
        {
            Directory.SetCurrentDirectory(_rootDirectory);

            Directory.Delete(TestTempFolder, true);

            foreach (var file in Directory.GetFiles(_rootDirectory))
            {
                if (file.EndsWith(".zip"))
                {
                    File.Delete(file);
                }
            }
        }

        #endregion Test Preparations

        #region Tests

        /// <summary>
        /// Simple install and uninstall of a fix
        /// </summary>
        [Fact]
        public async Task InstallUninstallFixTest()
        {  
            await InstallFixAsync(fixEntity: _fileFixEntity, variant: null);

            UninstallFix(_fileFixEntity);
        }

        /// <summary>
        /// Install and uninstall fix variant
        /// </summary>
        [Fact]
        public async Task InstallUninstallFixVariantTest()
        {
            await InstallFixAsync(fixEntity: _fileFixEntity, variant: "variant2");

            UninstallFix(_fileFixEntity);
        }

        /// <summary>
        /// Install, update and uninstall fix
        /// </summary>
        [Fact]
        public async Task UpdateFixTest()
        {
            await InstallFixAsync(fixEntity: _fileFixEntity, variant: null);

            await UpdateFixAsync(_gameEntity, _fileFixEntity);

            UninstallFix(_fileFixEntity);
        }

        /// <summary>
        /// Install fix with incorrect MD5
        /// </summary>
        [Fact]
        public async Task InstallCompromisedFixTest()
        {
            FileFixEntity fixEntity = new()
            {
                Name = "test fix compromised",
                Version = 1,
                Guid = Guid.Parse("C0650F19-F670-4F8A-8545-70F6C5171FA5"),
                Url = "https://github.com/fgsfds/SteamFD-Fixes-Repo/raw/master/fixes/bsp_nointro_v1.zip",
                MD5 = "badMD5"
            };

            var installResult = await _fixManager.InstallFixAsync(_gameEntity, fixEntity, null, false);

            Assert.False(File.Exists(Consts.InstalledFile));
            Assert.True(installResult == ResultEnum.MD5Error);
        }

        /// <summary>
        /// Install fix to a new folder
        /// </summary>
        [Fact]
        public async Task InstallFixToANewFolderTest()
        {
            File.Copy(
                Path.Combine(_rootDirectory, $"Resources\\{TestFixZip}"), 
                Path.Combine(_rootDirectory, TestFixZip), 
                true
                );

            FileFixEntity fixEntity = new()
            {
                Name = "test fix new folder",
                Version = 1,
                Guid = Guid.Parse("C0650F19-F670-4F8A-8545-70F6C5171FA5"),
                Url = TestFixZip,
                InstallFolder = "new folder"
            };

            await _fixManager.InstallFixAsync(_gameEntity, fixEntity, null, true);

            Assert.True(File.Exists("game\\new folder\\start game.exe"));

            _fixManager.UninstallFix(_gameEntity, fixEntity);

            Assert.False(Directory.Exists("game\\new folder"));
        }

        /// <summary>
        /// Install and uninstall fix that includes octodiff patch
        /// </summary>
        [Fact]
        public async Task InstallFixWithPatching()
        {
            File.Copy(
                Path.Combine(_rootDirectory, $"Resources\\{TestFixPatchZip}"),
                Path.Combine(_rootDirectory, TestFixPatchZip),
                true
                );

            FileFixEntity fixEntity = new()
            {
                Name = "test fix with patch",
                Version = 1,
                Guid = Guid.Parse("C0650F19-F670-4F8A-8545-70F6C5171FA5"),
                Url = TestFixPatchZip,
                InstallFolder = "install folder",
                FilesToPatch = ["install folder\\start game.exe"]
            };

            await _fixManager.InstallFixAsync(_gameEntity, fixEntity, null, true);

            var installedActual = File.ReadAllText(Consts.InstalledFile);
            var installedExpected = $@"[
  {{
    ""$type"": ""FileFix"",
    ""BackupFolder"": ""test_fix_with_patch"",
    ""FilesList"": [
      ""install folder\\start game.exe.octodiff""
    ],
    ""InstalledSharedFix"": null,
    ""GameId"": 1,
    ""Guid"": ""c0650f19-f670-4f8a-8545-70f6c5171fa5"",
    ""Version"": 1
  }}
]";

            var exeActual = File.ReadAllText("game\\install folder\\start game.exe");
            var exeExpected = "original_patched";

            Assert.Equal(installedActual, installedExpected);
            Assert.Equal(exeActual, exeExpected);

            UninstallFix(fixEntity);
        }

        #endregion Tests

        #region Private Methods

        private async Task InstallFixAsync(FileFixEntity fixEntity, string? variant)
        {
            var fixArchive = variant is null ? TestFixZip : TestFixVariantZip;

            File.Copy(
                Path.Combine(_rootDirectory, $"Resources\\{fixArchive}"),
                Path.Combine(_rootDirectory, TestFixZip),
                true
                );

            await _fixManager.InstallFixAsync(_gameEntity, fixEntity, variant, true);

            CheckNewFiles();

            //modify backed up file
            await File.WriteAllTextAsync("game\\install folder\\file to backup.txt", "modified");
        }

        private async Task UpdateFixAsync(GameEntity gameEntity, FileFixEntity fileFix)
        {
            File.Copy(
                Path.Combine(_rootDirectory, $"Resources\\{TestFixV2Zip}"),
                Path.Combine(_rootDirectory, TestFixV2Zip),
                true
                );

            FileFixEntity newFileFix = new()
            {
                Name = "test fix",
                Version = 2,
                Guid = Guid.Parse("C0650F19-F670-4F8A-8545-70F6C5171FA5"),
                Url = TestFixV2Zip,
                InstallFolder = "install folder",
                FilesToDelete = ["install folder\\file to delete.txt", "install folder\\subfolder\\file to delete in subfolder.txt", "file to delete in parent folder.txt"],
                FilesToBackup = ["install folder\\file to backup.txt"],
                InstalledFix = fileFix.InstalledFix
            };

            await _fixManager.UpdateFixAsync(gameEntity, newFileFix, null, true);

            fileFix.InstalledFix = newFileFix.InstalledFix;

            CheckUpdatedFiles();
        }

        private void UninstallFix(FileFixEntity fixEntity)
        {
            _fixManager.UninstallFix(_gameEntity, fixEntity);

            CheckOriginalFiles();
        }

        private static void CheckOriginalFiles()
        {
            //check original files
            var exeExists = File.Exists("game\\install folder\\start game.exe");
            Assert.True(exeExists);

            var fi = File.ReadAllText("game\\install folder\\start game.exe");
            Assert.Equal("original", fi);

            var fileExists = File.Exists("game\\install folder\\subfolder\\file.txt");
            Assert.True(fileExists);

            var fi2 = File.ReadAllText("game\\install folder\\subfolder\\file.txt");
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

            var fi3 = File.ReadAllText("game\\install folder\\file to backup.txt");
            Assert.Equal("original", fi3);
        }

        private static void CheckNewFiles()
        {
            //check replaced files
            var exeExists = File.Exists("game\\install folder\\start game.exe");
            Assert.True(exeExists);

            var fi = File.ReadAllText("game\\install folder\\start game.exe");
            Assert.Equal("fix_v1", fi);

            var fileExists = File.Exists("game\\install folder\\subfolder\\file.txt");
            Assert.True(fileExists);

            var fi2 = File.ReadAllText("game\\install folder\\subfolder\\file.txt");
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
            var textActual = File.ReadAllText(Consts.InstalledFile);
            var textExpected = @$"[
  {{
    ""$type"": ""FileFix"",
    ""BackupFolder"": ""test_fix"",
    ""FilesList"": [
      ""install folder\\start game.exe"",
      ""install folder\\subfolder\\file.txt""
    ],
    ""InstalledSharedFix"": null,
    ""GameId"": 1,
    ""Guid"": ""c0650f19-f670-4f8a-8545-70f6c5171fa5"",
    ""Version"": 1
  }}
]";

            Assert.Equal(textActual, textExpected);
        }

        private static void CheckUpdatedFiles()
        {
            //check replaced files
            var exeExists = File.Exists("game\\install folder\\start game.exe");
            Assert.True(exeExists);

            var fi = File.ReadAllText("game\\install folder\\start game.exe");
            Assert.Equal("fix_v2", fi);

            var fileExists = File.Exists("game\\install folder\\subfolder\\file.txt");
            Assert.True(fileExists);

            var fi2 = File.ReadAllText("game\\install folder\\subfolder\\file.txt");
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

            //check installed.xml
            var textActual = File.ReadAllText(Consts.InstalledFile);
            var textExpected = @$"[
  {{
    ""$type"": ""FileFix"",
    ""BackupFolder"": ""test_fix"",
    ""FilesList"": [
      ""install folder\\start game.exe""
    ],
    ""InstalledSharedFix"": null,
    ""GameId"": 1,
    ""Guid"": ""c0650f19-f670-4f8a-8545-70f6c5171fa5"",
    ""Version"": 2
  }}
]";

            Assert.Equal(textActual, textExpected);
        }

        private string PrepareGameFolder()
        {
            var gameFolder = Path.Combine(_testDirectory, "game");

            ZipFile.ExtractToDirectory(Path.Combine(_rootDirectory, "Resources\\test_game.zip"), gameFolder);

            return gameFolder;
        }

        #endregion Private Methods
    }
}