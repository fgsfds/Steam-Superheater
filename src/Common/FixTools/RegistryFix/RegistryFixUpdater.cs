using Common.Entities;
using Common.Entities.Fixes;
using Common.Entities.Fixes.RegistryFix;

namespace Common.FixTools.RegistryFix
{
    public sealed class RegistryFixUpdater(
        RegistryFixInstaller _fixInstaller,
        RegistryFixUninstaller _fixUninstaller
        )
    {
        public BaseInstalledFixEntity UpdateFix(GameEntity game, RegistryFixEntity regFix)
        {
            _fixUninstaller.UninstallFix(regFix);

            var result = _fixInstaller.InstallFix(game, regFix);

            return result;
        }
    }
}
