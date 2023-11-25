using Common.Config;
using Common.DI;
using Common.Entities;
using Common.Entities.Fixes;
using Common.Entities.Fixes.HostsFix;
using Common.FixTools.HostsFix;
using Common.Providers;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Tests
{
    /// <summary>
    /// Tests that use instance data and should be run in a single thread
    /// </summary>
    [Collection("Sync")]
    public sealed class HostsFixTests : IDisposable
    {
        private readonly HostsFixInstaller _hostsFixInstaller;
        private readonly HostsFixUninstaller _hostsFixUninstaller;
        private readonly string _hostsFilePath = Path.Combine(Directory.GetCurrentDirectory(), "hosts");

        private readonly GameEntity _gameEntity = new()
        {
            Id = 1,
            Name = "test game",
            InstallDir = "C:\\games\\test game\\"
        };

        private readonly HostsFixEntity _fixEntity = new()
        {
            Name = "test fix",
            Version = 1,
            Guid = Guid.Parse("C0650F19-F670-4F8A-8545-70F6C5171FA5"),
            Entries = ["123 added entry "]
        };

        #region Test Preparations

        public HostsFixTests()
        {
            BindingsManager.Reset();
            var container = BindingsManager.Instance;
            container.AddScoped<ConfigProvider>();
            CommonBindings.Load(container);

            _hostsFixInstaller = BindingsManager.Provider.GetRequiredService<HostsFixInstaller>();
            _hostsFixUninstaller = BindingsManager.Provider.GetRequiredService<HostsFixUninstaller>();

            File.Copy($"Resources\\hosts", _hostsFilePath, true);

        }

        public void Dispose()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return;
            }

            File.Delete(_hostsFilePath);
        }

        #endregion Test Preparations

        #region Tests

        [Fact]
        public void InstallUninstallFixTest()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Assert.Fail();
                return;
            }

            //Preparations
            var gameEntity = _gameEntity;
            var fixEntity = _fixEntity;

            //Install Fix
            var installedFix = _hostsFixInstaller.InstallFix(gameEntity, fixEntity, _hostsFilePath);

            InstalledFixesProvider.SaveInstalledFixes(new List<BaseInstalledFixEntity>() { installedFix });

            if (installedFix is not HostsInstalledFixEntity)
            {
                Assert.Fail();
                return;
            }

            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(_hostsFilePath))
                {
                    var hash = Convert.ToHexString(md5.ComputeHash(stream));

                    Assert.Equal("F00C5C32FFAD7C21CAF5F436B204D2E7", hash);
                }
            }

            fixEntity.InstalledFix = installedFix;

            _hostsFixUninstaller.UninstallFix(fixEntity, _hostsFilePath);

            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(_hostsFilePath))
                {
                    var hash = Convert.ToHexString(md5.ComputeHash(stream));

                    Assert.Equal("B431935EBF5DA06DC87E5032454F5E29", hash);
                }
            }
        }

        #endregion Tests
    }
}