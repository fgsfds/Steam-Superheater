//using Common.Config;
//using Common.DI;
//using Common.Entities;
//using Common.Entities.Fixes.RegistryFix;
//using Common.Enums;
//using Common.FixTools;
//using Common.Helpers;
//using Common.Providers;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Win32;
//using System.Runtime.InteropServices;
//using System.Security.Cryptography;

//namespace Tests
//{
//    /// <summary>
//    /// Tests that use instance data and should be run in a single thread
//    /// </summary>
//    [Collection("Sync")]
//    public sealed class RegistryFixTests : IDisposable
//    {
//        private const string GameDir = "C:\\games\\test game\\";
//        private const string GameExe = "game exe.exe";
//        private const string RegKey = "SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\AppCompatFlags\\Layers_test";

//        private readonly FixManager _fixManager;
//        private readonly InstalledFixesProvider _installedFixesProvider;

//        private readonly GameEntity _gameEntity = new()
//        {
//            Id = 1,
//            Name = "test game",
//            InstallDir = GameDir
//        };

//        private readonly RegistryFixEntity _fixEntity = new()
//        {
//            Name = "test fix",
//            Version = 1,
//            Guid = Guid.Parse("C0650F19-F670-4F8A-8545-70F6C5171FA5"),
//            Key = "HKEY_CURRENT_USER\\" + RegKey,
//            ValueName = "{gamefolder}\\" + GameExe,
//            NewValueData = "~ RUNASADMIN",
//            ValueType = RegistryValueTypeEnum.String,
//            SupportedOSes = OSEnum.Windows
//        };

//        #region Test Preparations

//        public RegistryFixTests()
//        {
//            BindingsManager.Reset();
//            var container = BindingsManager.Instance;
//            container.AddScoped<ConfigProvider>();
//            container.AddScoped<InstalledFixesProvider>();
//            CommonBindings.Load(container);

//            _fixManager = BindingsManager.Provider.GetRequiredService<FixManager>();
//            _installedFixesProvider = BindingsManager.Provider.GetRequiredService<InstalledFixesProvider>();
//        }

//        public void Dispose()
//        {
//            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
//            {
//                return;
//            }

//            var dir = Directory.GetCurrentDirectory();

//            using (var key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\AppCompatFlags", true))
//            {
//                if (key is null)
//                {
//                    return;
//                }

//                key.DeleteSubKey("Layers_test");
//            }

//            File.Delete(Path.Combine(dir, Consts.ConfigFile));
//            File.Delete(Path.Combine(dir, Consts.InstalledFile));
//        }

//        #endregion Test Preparations

//        #region Tests

//        [Fact]
//        public async Task InstallUninstallFixTest()
//        {
//            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
//            {
//                Assert.Fail();
//                return;
//            }

//            //Preparations
//            var gameEntity = _gameEntity;
//            var fixEntity = _fixEntity;

//            //Install Fix
//            await _fixManager.InstallFixAsync(gameEntity, fixEntity, null, true);

//            await _installedFixesProvider.SaveInstalledFixesAsync();

//            //Check installed.fix hash
//            using (var md5 = MD5.Create())
//            {
//                await using (var stream = File.OpenRead(Consts.InstalledFile))
//                {
//                    var hash = Convert.ToHexString(await md5.ComputeHashAsync(stream));

//                    Assert.Equal("CDF4CB5E941EDBBB1984977DAD4D2F29", hash);
//                }
//            }

//            //Check if registry value is created
//            installedRegFix.Key.Replace("HKEY_CURRENT_USER\\", string.Empty);
//            using (var key = Registry.CurrentUser.OpenSubKey(newKey, true))
//            {
//                Assert.NotNull(key);

//                var value = (string?)key.GetValue(installedRegFix.ValueName, null);

//                Assert.Equal(fixEntity.NewValueData, value);
//            }

//            //Uninstall fix
//            _fixManager.UninstallFix(gameEntity, fixEntity);

//            //Check if registry value is removed
//            using (var key = Registry.CurrentUser.OpenSubKey(newKey, true))
//            {
//                Assert.NotNull(key);

//                var value = key.GetValue(installedRegFix.ValueName, null);

//                Assert.Null(value);
//            }
//        }

//        [Fact]
//        public async Task InstallUninstallReplaceFixTest()
//        {
//            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
//            {
//                Assert.Fail();
//                return;
//            }

//            const string OldValue = "OLD VALUE";

//            //Preparations
//            using (var key = Registry.CurrentUser.CreateSubKey(RegKey))
//            {
//                key.SetValue(GameDir + GameExe, OldValue);
//            }

//            var gameEntity = _gameEntity;
//            var fixEntity = _fixEntity;

//            //Install Fix
//            await _fixManager.InstallFixAsync(gameEntity, fixEntity, null, true);

//            await _installedFixesProvider.SaveInstalledFixesAsync();

//            //Check installed.fix hash
//            using (var md5 = MD5.Create())
//            {
//                await using (var stream = File.OpenRead(Consts.InstalledFile))
//                {
//                    var hash = Convert.ToHexString(await md5.ComputeHashAsync(stream));

//                    Assert.Equal("9E0AC8E1BCF3CECDACDBDE29AFA3818E", hash);
//                }
//            }

//            //Check if registry value is set
//            installedRegFix.Key.Replace("HKEY_CURRENT_USER\\", string.Empty);
//            using (var key = Registry.CurrentUser.OpenSubKey(newKey, true))
//            {
//                Assert.NotNull(key);

//                var value = (string?)key.GetValue(installedRegFix.ValueName, null);

//                Assert.Equal(fixEntity.NewValueData, value);
//            }

//            //Uninstall fix
//            _fixManager.UninstallFix(gameEntity, fixEntity);

//            //Check if registry value is reverted
//            using (var key = Registry.CurrentUser.OpenSubKey(newKey, true))
//            {
//                Assert.NotNull(key);

//                var value = (string?)key.GetValue(installedRegFix.ValueName, null);

//                Assert.Equal(OldValue, value);
//            }
//        }

//        #endregion Tests
//    }
//}