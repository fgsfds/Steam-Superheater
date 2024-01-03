using Common.Entities;
using Common.Entities.Fixes;
using Common.Entities.Fixes.FileFix;
using Common.Entities.Fixes.HostsFix;
using Common.Entities.Fixes.RegistryFix;
using Common.FixTools.FileFix;
using Common.FixTools.HostsFix;
using Common.FixTools.RegistryFix;
using Common.Helpers;
using Common.Providers;

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
        HostsFixUpdater hostsFixUpdater,

        InstalledFixesProvider installedFixesProvider
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

        private readonly InstalledFixesProvider _installedFixesProvider = installedFixesProvider;

        public async Task<Result> InstallFixAsync(GameEntity game, BaseFixEntity fix, string? variant, bool skipMD5Check)
        {
            Logger.Info($"Installing {fix.Name} for {game.Name}");

            BaseInstalledFixEntity? installedFix = null;

            try
            {
                switch (fix)
                {
                    case FileFixEntity fileFix:
                        installedFix = await _fileFixInstaller.InstallFixAsync(game, fileFix, variant, skipMD5Check);
                        break;
                    case RegistryFixEntity registryFix:
                        installedFix = _registryFixInstaller.InstallFix(game, registryFix);
                        break;
                    case HostsFixEntity hostsFix:
                        installedFix = _hostsFixInstaller.InstallFix(game, hostsFix);
                        break;
                    default:
                        ThrowHelper.NotImplementedException<BaseInstalledFixEntity>("Installer for this fix type is not implemented");
                        break;
                }
            }
            catch (HashCheckFailedException ex)
            {
                Logger.Error(ex.Message);
                return new(ResultEnum.MD5Error, "MD5 of the file doesn't match the database");
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
                return new(ResultEnum.Error, ex.Message);
            }

            fix.InstalledFix = installedFix;
            _installedFixesProvider.AddToCache(installedFix);

            var saveResult = _installedFixesProvider.SaveInstalledFixes();

            if (!saveResult.IsSuccess)
            {
                return saveResult;
            }

            return new(ResultEnum.Success, "Fix installed successfully!");
        }

        public Result UninstallFix(GameEntity game, BaseFixEntity fix)
        {
            Logger.Info($"Uninstalling {fix.Name} for {game.Name}");

            try
            {
                switch (fix)
                {
                    case FileFixEntity fileFix:
                        _fileFixUninstaller.UninstallFix(game, fileFix.InstalledFix, fileFix.Url);
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
            catch (BackwardsCompatibilityException ex)
            {
                Logger.Error(ex.Message);
                fix.InstalledFix = null;

                var saveResultInner = _installedFixesProvider.SaveInstalledFixes();

                if (!saveResultInner.IsSuccess)
                {
                    return saveResultInner;
                }

                return new(ResultEnum.BackwardsCompatibility, "Error while restoring backed up files. Verify integrity of game files on Steam.");
            }
            catch (IOException ex)
            {
                Logger.Error(ex.Message);
                return new(ResultEnum.FileAccessError, ex.Message);
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
                return new(ResultEnum.Error, ex.Message);
            }

            fix.InstalledFix = null;
            _installedFixesProvider.RemoveFromCache(game.Id, fix.Guid);

            var saveResult = _installedFixesProvider.SaveInstalledFixes();

            if (!saveResult.IsSuccess)
            {
                return saveResult;
            }

            return new(ResultEnum.Success, "Fix uninstalled successfully!");
        }

        public async Task<Result> UpdateFixAsync(GameEntity game, BaseFixEntity fix, string? variant, bool skipMD5Check)
        {
            Logger.Info($"Updating {fix.Name} for {game.Name}");

            BaseInstalledFixEntity? installedFix = null;

            try
            {
                switch (fix)
                {
                    case FileFixEntity fileFix:
                        installedFix = await _fileFixUpdater.UpdateFixAsync(game, fileFix, variant, skipMD5Check);
                        break;
                    case RegistryFixEntity registryFix:
                        installedFix = _registryFixUpdater.UpdateFix(game, registryFix);
                        break;
                    case HostsFixEntity hostsFix:
                        installedFix = _hostsFixUpdater.UpdateFix(game, hostsFix);
                        break;
                    default:
                        ThrowHelper.NotImplementedException<BaseInstalledFixEntity>("Updater for this fix type is not implemented");
                        break;
                }
            }
            catch (BackwardsCompatibilityException ex)
            {
                Logger.Error(ex.Message);
                fix.InstalledFix = null;

                var saveResultInner = _installedFixesProvider.SaveInstalledFixes();

                if (!saveResultInner.IsSuccess)
                {
                    return saveResultInner;
                }

                return new(ResultEnum.BackwardsCompatibility, "Error while restoring backed up files. Verify integrity of game files on Steam.");
            }
            catch (HashCheckFailedException ex)
            {
                Logger.Error(ex.Message);
                return new(ResultEnum.MD5Error, "MD5 of the file doesn't match the database");
            }
            catch (IOException ex)
            {
                Logger.Error(ex.Message);
                return new(ResultEnum.FileAccessError, ex.Message);
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
                return new(ResultEnum.Error, ex.Message);
            }

            fix.InstalledFix = installedFix;
            _installedFixesProvider.RemoveFromCache(game.Id, fix.Guid);
            _installedFixesProvider.AddToCache(installedFix);

            var saveResult = _installedFixesProvider.SaveInstalledFixes();

            if (!saveResult.IsSuccess)
            {
                return saveResult;
            }

            return new(ResultEnum.Success, "Fix updated successfully!");
        }
    }
}
