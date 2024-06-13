using Common.Entities;
using Common.Entities.Fixes;
using Common.Entities.Fixes.RegistryFix;
using Common.Helpers;

namespace Common.Client.FixTools.RegistryFix
{
    public sealed class RegistryFixUpdater
    {
        private readonly RegistryFixInstaller _fixInstaller;
        private readonly RegistryFixUninstaller _fixUninstaller;


        public RegistryFixUpdater(
            RegistryFixInstaller fixInstaller,
            RegistryFixUninstaller fixUninstaller
            )
        {
            _fixInstaller = fixInstaller;
            _fixUninstaller = fixUninstaller;
        }


        public Result<BaseInstalledFixEntity> UpdateFix(GameEntity game, RegistryFixEntity regFix)
        {
            regFix.InstalledFix.ThrowIfNull();

            _fixUninstaller.UninstallFix(regFix.InstalledFix);

            var result = _fixInstaller.InstallFix(game, regFix);

            return result;
        }
    }
}
