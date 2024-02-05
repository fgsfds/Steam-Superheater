using Common.Entities;
using Common.Entities.Fixes;
using Common.Entities.Fixes.RegistryFix;
using Common.Helpers;

namespace Common.FixTools.RegistryFix
{
    public sealed class RegistryFixUpdater(
        RegistryFixInstaller fixInstaller,
        RegistryFixUninstaller fixUninstaller
        )
    {
        private readonly RegistryFixInstaller _fixInstaller = fixInstaller;
        private readonly RegistryFixUninstaller _fixUninstaller = fixUninstaller;

        public BaseInstalledFixEntity UpdateFix(GameEntity game, RegistryFixEntity regFix)
        {
            regFix.InstalledFix.ThrowIfNull();

            _fixUninstaller.UninstallFix(regFix.InstalledFix);

            var result = _fixInstaller.InstallFix(game, regFix);

            return result;
        }
    }
}
