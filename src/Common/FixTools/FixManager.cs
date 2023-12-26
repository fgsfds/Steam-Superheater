using Common.Entities;
using Common.Entities.Fixes;
using Common.Entities.Fixes.FileFix;
using Common.Entities.Fixes.HostsFix;
using Common.Entities.Fixes.RegistryFix;
using Common.FixTools.FileFix;
using Common.FixTools.HostsFix;
using Common.FixTools.RegistryFix;
using Common.Helpers;

namespace Common.FixTools
{
    public sealed class FixManager(
        FileFixInstaller fileFixInstaller,
        FileFixUninstaller fileFixUninstaller,
        FileFixUpdater fileFixUpdater,

        RegistryFixInstaller registryFixInstaller,
        RegistryFixUninstaller registryFixUninstaller,
        RegistryFixUpdater registryFixUpdater,

        HostsFixInstaller hostsFixInstaller,
        HostsFixUninstaller hostsFixUninstaller,
        HostsFixUpdater hostsFixUpdater
        )
    {
        private readonly FileFixInstaller _fileFixInstaller = fileFixInstaller;
        private readonly FileFixUninstaller _fileFixUninstaller = fileFixUninstaller;
        private readonly FileFixUpdater _fileFixUpdater = fileFixUpdater;

        private readonly RegistryFixInstaller _registryFixInstaller = registryFixInstaller;
        private readonly RegistryFixUninstaller _registryFixUninstaller = registryFixUninstaller;
        private readonly RegistryFixUpdater _registryFixUpdater = registryFixUpdater;

        private readonly HostsFixInstaller _hostsFixInstaller = hostsFixInstaller;
        private readonly HostsFixUninstaller _hostsFixUninstaller = hostsFixUninstaller;
        private readonly HostsFixUpdater _hostsFixUpdater = hostsFixUpdater;

        public async Task<BaseInstalledFixEntity> InstallFixAsync(GameEntity game, BaseFixEntity fix, string? variant, bool skipMD5Check)
        {
            Logger.Info($"Installing {fix.Name} for {game.Name}");

            switch (fix)
            {
                case FileFixEntity fileFix:
                    return await _fileFixInstaller.InstallFixAsync(game, fileFix, variant, skipMD5Check);
                case RegistryFixEntity registryFix:
                    return _registryFixInstaller.InstallFix(game, registryFix);
                case HostsFixEntity hostsFix:
                    return _hostsFixInstaller.InstallFix(game, hostsFix);
                default:
                    return ThrowHelper.NotImplementedException<BaseInstalledFixEntity>("Installer for this fix type is not implemented");
            }
        }

        public void UninstallFix(GameEntity game, BaseFixEntity fix)
        {
            Logger.Info($"Uninstalling {fix.Name} for {game.Name}");

            switch (fix)
            {
                case FileFixEntity fileFix:
                    _fileFixUninstaller.UninstallFix(game, fileFix);
                    break;
                case RegistryFixEntity regFix:
                    _registryFixUninstaller.UninstallFix(regFix);
                    break;
                case HostsFixEntity hostsFix:
                    _hostsFixUninstaller.UninstallFix(hostsFix);
                    break;
                default:
                    ThrowHelper.NotImplementedException("Uninstaller for this fix type is not implemented");
                    break;
            }
        }

        public async Task<BaseInstalledFixEntity> UpdateFixAsync(GameEntity game, BaseFixEntity fix, string? variant, bool skipMD5Check)
        {
            Logger.Info($"Updating {fix.Name} for {game.Name}");

            switch (fix)
            {
                case FileFixEntity fileFix:
                    return await _fileFixUpdater.UpdateFixAsync(game, fileFix, variant, skipMD5Check);
                case RegistryFixEntity registryFix:
                    return _registryFixUpdater.UpdateFix(game, registryFix);
                case HostsFixEntity hostsFix:
                    return _hostsFixUpdater.UpdateFix(game, hostsFix);
                default:
                    return ThrowHelper.NotImplementedException<BaseInstalledFixEntity>("Updater for this fix type is not implemented");
            }
        }
    }
}
