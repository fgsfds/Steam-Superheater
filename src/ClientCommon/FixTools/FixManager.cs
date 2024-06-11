using ClientCommon.FixTools.FileFix;
using ClientCommon.FixTools.HostsFix;
using ClientCommon.FixTools.RegistryFix;
using ClientCommon.Providers;
using Common;
using Common.Entities;
using Common.Entities.Fixes;
using Common.Entities.Fixes.FileFix;
using Common.Entities.Fixes.HostsFix;
using Common.Entities.Fixes.RegistryFix;
using Common.Helpers;

namespace ClientCommon.FixTools
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

        InstalledFixesProvider installedFixesProvider,

        Logger logger
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

        private readonly Logger _logger = logger;

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
                return new(ResultEnum.NotFound, $"""
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

            fix.InstalledFix = installedFix.ResultObject;
            _installedFixesProvider.AddToCache(installedFix.ResultObject!);

            var saveResult = _installedFixesProvider.SaveInstalledFixes();

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
            _installedFixesProvider.RemoveFromCache(game.Id, fix.Guid);

            var saveResult = _installedFixesProvider.SaveInstalledFixes();

            if (!saveResult.IsSuccess)
            {
                return saveResult;
            }

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

            fix.InstalledFix = installedFix.ResultObject;
            _installedFixesProvider.RemoveFromCache(game.Id, fix.Guid);
            _installedFixesProvider.AddToCache(installedFix.ResultObject!);

            var saveResult = _installedFixesProvider.SaveInstalledFixes();

            if (!saveResult.IsSuccess)
            {
                return saveResult;
            }

            return new(ResultEnum.Success, "Fix updated successfully!");
        }
    }
}
