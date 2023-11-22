using Common.Entities;
using Common.Entities.Fixes;
using Common.Entities.Fixes.FileFix;
using Common.Entities.Fixes.RegistryFix;
using Common.FixTools.FileFix;
using Common.FixTools.RegistryFix;
using Common.Helpers;

namespace Common.FixTools
{
    public class FixManager(
        FileFixInstaller fileFixInstaller,
        FileFixUninstaller fileFixUninstaller,
        FileFixUpdater fileFixUpdater,
        RegistryFixInstaller registryFixInstaller,
        RegistryFixUninstaller registryFixUninstaller,
        RegistryFixUpdater registryFixUpdater
        )
    {
        private readonly FileFixInstaller _fileFixInstaller = fileFixInstaller ?? ThrowHelper.NullReferenceException<FileFixInstaller>(nameof(fileFixInstaller));
        private readonly FileFixUninstaller _fileFixUninstaller = fileFixUninstaller ?? ThrowHelper.NullReferenceException<FileFixUninstaller>(nameof(fileFixUninstaller));
        private readonly FileFixUpdater _fileFixUpdater = fileFixUpdater ?? ThrowHelper.NullReferenceException<FileFixUpdater>(nameof(fileFixUpdater));

        private readonly RegistryFixInstaller _registryFixInstaller = registryFixInstaller ?? ThrowHelper.NullReferenceException<RegistryFixInstaller>(nameof(registryFixInstaller));
        private readonly RegistryFixUninstaller _registryFixUninstaller = registryFixUninstaller ?? ThrowHelper.NullReferenceException<RegistryFixUninstaller>(nameof(registryFixUninstaller));
        private readonly RegistryFixUpdater _registryFixUpdater = registryFixUpdater ?? ThrowHelper.NullReferenceException<RegistryFixUpdater>(nameof(registryFixUpdater));

        public async Task<BaseInstalledFixEntity> InstallFixAsync(GameEntity game, BaseFixEntity fix, string? variant, bool skipMD5Check)
        {
            Logger.Info($"Installing {fix.Name} for {game.Name}");

            if (fix is FileFixEntity fileFix)
            {
                return await _fileFixInstaller.InstallFixAsync(game, fileFix, variant, skipMD5Check);
            }
            else if (fix is RegistryFixEntity registryFix)
            {
                return _registryFixInstaller.InstallFix(game, registryFix);
            }
            else
            {
                return ThrowHelper.NotImplementedException<BaseInstalledFixEntity>("Installer for this fix type is not implemented");
            }
        }

        public void UninstallFix(GameEntity game, BaseFixEntity fix)
        {
            Logger.Info($"Uninstalling {fix.Name} for {game.Name}");

            if (fix is FileFixEntity fileFix)
            {
                _fileFixUninstaller.UninstallFix(game, fileFix);
            }
            else if (fix is RegistryFixEntity regFix)
            {
                _registryFixUninstaller.UninstallFix(regFix);
            }
            else
            {
                ThrowHelper.NotImplementedException("Uninstaller for this fix type is not implemented");
            }
        }

        public async Task<BaseInstalledFixEntity> UpdateFixAsync(GameEntity game, BaseFixEntity fix, string? variant, bool skipMD5Check)
        {
            Logger.Info($"Updating {fix.Name} for {game.Name}");

            if (fix is FileFixEntity fileFix)
            {
                return await _fileFixUpdater.UpdateFixAsync(game, fileFix, variant, skipMD5Check);
            }
            else if (fix is RegistryFixEntity registryFix)
            {
                return _registryFixUpdater.UpdateFix(game, registryFix);
            }
            else
            {
                return ThrowHelper.NotImplementedException<BaseInstalledFixEntity>("Updater for this fix type is not implemented");
            }
        }
    }
}
