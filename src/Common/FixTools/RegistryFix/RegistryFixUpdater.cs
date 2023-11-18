using Common.Entities;
using Common.Entities.Fixes;
using Common.Entities.Fixes.RegistryFix;

namespace Common.FixTools.RegistryFix
{
    public static class RegistryFixUpdater
        //RegistryFixInstaller fixInstaller,
        //RegistryFixUninstaller fixUninstaller
    {
        //private readonly RegistryFixInstaller _fixInstaller = fixInstaller ?? ThrowHelper.ArgumentNullException<RegistryFixInstaller>(nameof(fixInstaller));
        //private readonly RegistryFixUninstaller _fixUninstaller = fixUninstaller ?? ThrowHelper.ArgumentNullException<RegistryFixUninstaller>(nameof(fixUninstaller));

        public static BaseInstalledFixEntity UpdateFix(GameEntity game, RegistryFixEntity regFix)
        {
            RegistryFixUninstaller.UninstallFix(regFix);

            var result = RegistryFixInstaller.InstallFix(game, regFix);

            return result;
        }
    }
}
