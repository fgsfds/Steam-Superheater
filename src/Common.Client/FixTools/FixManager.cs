using Common.Client.FixTools.FileFix;
using Common.Client.FixTools.HostsFix;
using Common.Client.FixTools.RegistryFix;
using Common.Client.Providers;
using Common.Entities;
using Common.Entities.Fixes;
using Common.Entities.Fixes.FileFix;
using Common.Entities.Fixes.HostsFix;
using Common.Entities.Fixes.RegistryFix;
using Common.Helpers;

namespace Common.Client.FixTools
{
    public sealed class FixManager
    {
        private readonly FileFixInstaller _fileFixInstaller;
        private readonly FileFixUninstaller _fileFixUninstaller;
        private readonly FileFixUpdater _fileFixUpdater;

        private readonly RegistryFixInstaller _registryFixInstaller;
        private readonly RegistryFixUninstaller _registryFixUninstaller;
        private readonly RegistryFixUpdater _registryFixUpdater;

        private readonly HostsFixInstaller _hostsFixInstaller;
        private readonly HostsFixUninstaller _hostsFixUninstaller;
        private readonly HostsFixUpdater _hostsFixUpdater;

        private readonly InstalledFixesProvider _installedFixesProvider;

        private readonly Logger _logger;


        public FixManager(
            FileFixInstaller fileFixInstaller,
            FileFixUninstaller fileFixUninstaller,
            FileFixUpdater fileFixUpdater,

            RegistryFixInstaller registryFixInstaller,
            RegistryFixUninstaller registryFixUninstaller,
            RegistryFixUpdater registryFixUpdater,

            HostsFixInstaller hostsFixInstaller,
            HostsFixUninstaller hostsFixUninstaller,
            HostsFixUpdater hostsFixUpdater,

            InstalledFixesProvider installedFixesProvider,

            Logger logger
            )
        {
            _fileFixInstaller = fileFixInstaller;
            _fileFixUninstaller = fileFixUninstaller;
            _fileFixUpdater = fileFixUpdater;
            _registryFixInstaller = registryFixInstaller;
            _registryFixUninstaller = registryFixUninstaller;
            _registryFixUpdater = registryFixUpdater;
            _hostsFixInstaller = hostsFixInstaller;
            _hostsFixUninstaller = hostsFixUninstaller;
            _hostsFixUpdater = hostsFixUpdater;
            _installedFixesProvider = installedFixesProvider;
            _logger = logger;
        }


        public async Task<Result> InstallFixAsync(
            GameEntity game,
            BaseFixEntity fix,
            string? variant,
            bool skipMD5Check,
            CancellationToken cancellationToken,
            string hostsFile = Consts.Hosts)
        {
            _logger.Info($"Installing {fix.Name} for {game.Name}");

            if (fix is FileFixEntity &&
                !Directory.Exists(game.InstallDir))
            {
                return new(
                    ResultEnum.NotFound, 
                    $"""
                    Game folder not found:
                    
                    {game.InstallDir}
                    """);
            }

            Result<BaseInstalledFixEntity> installedFix;

            switch (fix)
            {
                case FileFixEntity fileFix:
                    installedFix = await _fileFixInstaller.InstallFixAsync(game, fileFix, variant, skipMD5Check, cancellationToken).ConfigureAwait(false);
                    break;
                case RegistryFixEntity registryFix:
                    installedFix = _registryFixInstaller.InstallFix(game, registryFix);
                    break;
                case HostsFixEntity hostsFix:
                    installedFix = _hostsFixInstaller.InstallFix(game, hostsFix, hostsFile);
                    break;
                default:
                    return ThrowHelper.NotImplementedException<Result>("Installer for this fix type is not implemented");
            }

            if (!installedFix.IsSuccess)
            {
                return new(installedFix.ResultEnum, installedFix.Message);
            }

            if (installedFix.ResultObject is null)
            {
                return new(ResultEnum.Error, "Installed fix is null");
            }

            fix.InstalledFix = installedFix.ResultObject;

            var saveResult = _installedFixesProvider.CreateInstalledJson(game, installedFix.ResultObject);

            if (!saveResult.IsSuccess)
            {
                return saveResult;
            }

            return new(ResultEnum.Success, "Fix installed successfully!");
        }

        public Result UninstallFix(GameEntity game, BaseFixEntity fix, string hostsFile = Consts.Hosts)
        {
            _logger.Info($"Uninstalling {fix.Name} for {game.Name}");

            if (fix is FileFixEntity &&
                !Directory.Exists(game.InstallDir))
            {
                return new(ResultEnum.NotFound, $"Game folder not found: {Environment.NewLine + Environment.NewLine + game.InstallDir}");
            }

            try
            {
                switch (fix)
                {
                    case FileFixEntity fileFix:
                        fileFix.InstalledFix.ThrowIfNull();
                        _fileFixUninstaller.UninstallFix(game, fileFix.InstalledFix);
                        break;
                    case RegistryFixEntity regFix:
                        regFix.InstalledFix.ThrowIfNull();
                        _registryFixUninstaller.UninstallFix(regFix.InstalledFix);
                        break;
                    case HostsFixEntity hostsFix:
                        hostsFix.InstalledFix.ThrowIfNull();
                        _hostsFixUninstaller.UninstallFix(hostsFix.InstalledFix, hostsFile);
                        break;
                    default:
                        ThrowHelper.NotImplementedException("Uninstaller for this fix type is not implemented");
                        break;
                }
            }
            catch (IOException ex)
            {
                _logger.Error(ex.Message);
                return new(ResultEnum.FileAccessError, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message);
                return new(ResultEnum.Error, ex.Message);
            }

            fix.InstalledFix = null;

            var saveResult = _installedFixesProvider.RemoveInstalledJson(game, fix.Guid);

            if (!saveResult.IsSuccess)
            {
                return saveResult;
            }

            DeleteBackupFolderIfEmpty(game.InstallDir);

            return new(ResultEnum.Success, "Fix uninstalled successfully!");
        }

        public async Task<Result> UpdateFixAsync(GameEntity game, BaseFixEntity fix, string? variant, bool skipMD5Check, CancellationToken cancellationToken, string hostsFile = Consts.Hosts)
        {
            _logger.Info($"Updating {fix.Name} for {game.Name}");

            if (fix is FileFixEntity &&
                !Directory.Exists(game.InstallDir))
            {
                return new(ResultEnum.NotFound, $"Game folder not found: {Environment.NewLine + Environment.NewLine + game.InstallDir}");
            }

            Result<BaseInstalledFixEntity> installedFix;

            switch (fix)
            {
                case FileFixEntity fileFix:
                    installedFix = await _fileFixUpdater.UpdateFixAsync(game, fileFix, variant, skipMD5Check, cancellationToken).ConfigureAwait(false);
                    break;
                case RegistryFixEntity registryFix:
                    installedFix = _registryFixUpdater.UpdateFix(game, registryFix);
                    break;
                case HostsFixEntity hostsFix:
                    installedFix = _hostsFixUpdater.UpdateFix(game, hostsFix, hostsFile);
                    break;
                default:
                    return ThrowHelper.NotImplementedException<Result>("Updater for this fix type is not implemented");
            }

            if (!installedFix.IsSuccess)
            {
                return new(installedFix.ResultEnum, installedFix.Message);
            }

            if (installedFix.ResultObject is null)
            {
                return new(ResultEnum.Error, "Installed fix is null");
            }

            fix.InstalledFix = installedFix.ResultObject;

            var saveResult1 = _installedFixesProvider.RemoveInstalledJson(game, fix.Guid);

            if (!saveResult1.IsSuccess)
            {
                return saveResult1;
            }

            var saveResult2 = _installedFixesProvider.CreateInstalledJson(game, installedFix.ResultObject);

            if (!saveResult2.IsSuccess)
            {
                return saveResult2;
            }

            DeleteBackupFolderIfEmpty(game.InstallDir);

            return new(ResultEnum.Success, "Fix updated successfully!");
        }


        /// <summary>
        /// Delete backup folder if it's empty
        /// </summary>
        /// <param name="gameInstallDir">Game install folder</param>
        private static void DeleteBackupFolderIfEmpty(string gameInstallDir)
        {
            var backupFolder = Path.Combine(gameInstallDir, Consts.BackupFolder);

            if (Directory.Exists(backupFolder) &&
                Directory.GetFiles(backupFolder).Length == 0 &&
                Directory.GetDirectories(backupFolder).Length == 0)
            {
                Directory.Delete(backupFolder);
            }
        }
    }
}
