using Common.Entities;
using Common.Entities.Fixes;
using Common.Entities.Fixes.HostsFix;
using Common.Helpers;

namespace Common.Client.FixTools.HostsFix
{
    public sealed class HostsFixUpdater(
        HostsFixInstaller fixInstaller,
        HostsFixUninstaller fixUninstaller
        )
    {
        private readonly HostsFixInstaller _fixInstaller = fixInstaller;
        private readonly HostsFixUninstaller _fixUninstaller = fixUninstaller;

        public Result<BaseInstalledFixEntity> UpdateFix(GameEntity game, HostsFixEntity hostsFix, string hostsFile)
        {
            hostsFix.InstalledFix.ThrowIfNull();

            _fixUninstaller.UninstallFix(hostsFix.InstalledFix, hostsFile);

            var result = _fixInstaller.InstallFix(game, hostsFix, hostsFile);

            return result;
        }
    }
}
