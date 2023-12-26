using Common.Entities;
using Common.Entities.Fixes;
using Common.Entities.Fixes.RegistryFix;

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
            _fixUninstaller.UninstallFix(regFix);

            var result = _fixInstaller.InstallFix(game, regFix);

            return result;
        }
    }
}
