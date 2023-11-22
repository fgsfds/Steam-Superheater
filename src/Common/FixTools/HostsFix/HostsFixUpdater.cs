using Common.Entities;
using Common.Entities.Fixes;
using Common.Entities.Fixes.HostsFix;
using Common.Helpers;

namespace Common.FixTools.HostsFix
{
    public class HostsFixUpdater(
        HostsFixInstaller fixInstaller,
        HostsFixUninstaller fixUninstaller
        )
    {
        private readonly HostsFixInstaller _fixInstaller = fixInstaller ?? ThrowHelper.ArgumentNullException<HostsFixInstaller>(nameof(fixInstaller));
        private readonly HostsFixUninstaller _fixUninstaller = fixUninstaller ?? ThrowHelper.ArgumentNullException<HostsFixUninstaller>(nameof(fixUninstaller));

        public BaseInstalledFixEntity UpdateFix(GameEntity game, HostsFixEntity regFix)
        {
            _fixUninstaller.UninstallFix(regFix);

            var result = _fixInstaller.InstallFix(game, regFix);

            return result;
        }
    }
}
