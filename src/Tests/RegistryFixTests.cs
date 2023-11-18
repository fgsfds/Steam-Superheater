using Common.DI;
using Common.Entities;
using Common.FixTools;
using Common.Entities.Fixes.RegistryFix;
using Common.Entities.Fixes;
using Common.Providers;
using Microsoft.Win32;
using System.Runtime.InteropServices;

namespace Tests
{
    /// <summary>
    /// Tests that use instance data and should be run in a single thread
    /// </summary>
    [Collection("Sync")]
    public sealed class RegistryFixTests : IDisposable
    {
        private const string GameDir = "C:\\games\\test game\\";
        private const string GameExe = "game exe.exe";

        #region Test Preparations

        public RegistryFixTests()
        {
            BindingsManager.Reset();

            var container = BindingsManager.Instance;
            container.Options.EnableAutoVerification = false;
            container.Options.ResolveUnregisteredConcreteTypes = true;

            CommonBindings.Load(container);
            ProvidersBindings.Load(container);
        }

        public void Dispose()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) { return; }

            using (var key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\AppCompatFlags\\Layers", true))
            {
                if (key is null) { return; }

                var value = key.GetValue(GameDir + GameExe, null);

                if (value is not null)
                {
                    key.DeleteValue(GameDir + GameExe);
                }
            }
        }

        #endregion Test Preparations

        #region Tests

        [Fact]
        public async Task InstallUninstallFixTest()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) { Assert.Fail(); return; }

            //Preparations
            GameEntity gameEntity = new(
                1,
                "test game",
                GameDir
            );

            RegistryFixEntity fixEntity = new()
            {
                Name = "test fix",
                Version = 1,
                Guid = Guid.Parse("C0650F19-F670-4F8A-8545-70F6C5171FA5"),
                Key = "HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\AppCompatFlags\\Layers",
                ValueName = "{installfolder}\\" + GameExe,
                NewValueData = "~ RUNASADMIN"
            };

            var fixManager = BindingsManager.Instance.GetInstance<FixManager>();

            //Install Fix
            var installedFix = await fixManager.InstallFixAsync(gameEntity, fixEntity, null, true);

            InstalledFixesProvider.SaveInstalledFixes(new List<BaseInstalledFixEntity>() { installedFix });

            if (installedFix is not RegistryInstalledFixEntity installedRegFix)
            {
                Assert.Fail();
                return;
            }

            fixEntity.InstalledFix = installedFix;

            //Check if registry value is created
            var newKey = installedRegFix.Key.Replace("HKEY_CURRENT_USER\\", string.Empty);
            using (var key = Registry.CurrentUser.OpenSubKey(newKey, true))
            {
                Assert.NotNull(key);

                var value = (string?)key.GetValue(installedRegFix.ValueName, null);

                Assert.Equal(fixEntity.NewValueData, value);
            }

            //Uninstall fix
            fixManager.UninstallFix(gameEntity, fixEntity);

            //Check if registry value is removed
            using (var key = Registry.CurrentUser.OpenSubKey(newKey, true))
            {
                Assert.NotNull(key);

                var value = key.GetValue(installedRegFix.ValueName, null);

                Assert.Null(value);
            }
        }

        #endregion Tests

        #region Private Methods

        #endregion Private Methods
    }
}