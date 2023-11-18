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
        private readonly RegistryFixInstaller _fixInstaller = fixInstaller ?? ThrowHelper.ArgumentNullException<RegistryFixInstaller>(nameof(fixInstaller));
        private readonly RegistryFixUninstaller _fixUninstaller = fixUninstaller ?? ThrowHelper.ArgumentNullException<RegistryFixUninstaller>(nameof(fixUninstaller));

        public BaseInstalledFixEntity UpdateFix(GameEntity game, RegistryFixEntity regFix)
        {
            _fixUninstaller.UninstallFix(regFix);

            var result = _fixInstaller.InstallFix(game, regFix);

            return result;
        }
    }
}
