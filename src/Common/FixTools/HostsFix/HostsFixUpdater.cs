using Common.Entities;
using Common.Entities.Fixes;
using Common.Entities.Fixes.HostsFix;

namespace Common.FixTools.HostsFix
{
    public sealed class HostsFixUpdater(
        HostsFixInstaller _fixInstaller,
        HostsFixUninstaller _fixUninstaller
        )
    {
        public BaseInstalledFixEntity UpdateFix(GameEntity game, HostsFixEntity hostsFix)
        {
            _fixUninstaller.UninstallFix(hostsFix);

            var result = _fixInstaller.InstallFix(game, hostsFix);

            return result;
        }
    }
}
