using Common.Client.FixTools.FileFix;
using Common.Client.FixTools.HostsFix;
using Common.Client.FixTools.RegistryFix;
using Common.Client.Providers.Interfaces;
using Common.Entities;
using Common.Entities.Fixes;
using Common.Entities.Fixes.FileFix;
using Common.Entities.Fixes.HostsFix;
using Common.Entities.Fixes.RegistryFix;
using Common.Entities.Fixes.RegistryFixV2;
using Common.Helpers;
using CommunityToolkit.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Common.Client.FixTools;

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

    private readonly IInstalledFixesProvider _installedFixesProvider;

    private readonly ILogger _logger;


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

        IInstalledFixesProvider installedFixesProvider,

        ILogger logger
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
        string hostsFile = Consts.Hosts
        )
    {
        _logger.LogInformation($"Installing {fix.Name} for {game.Name}");

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
            case RegistryFixV2Entity registryFix2:
                installedFix = _registryFixInstaller.InstallFix(game, registryFix2);
                break;
            case HostsFixEntity hostsFix:
                installedFix = _hostsFixInstaller.InstallFix(game, hostsFix, hostsFile);
                break;
            default:
                return ThrowHelper.ThrowNotSupportedException<Result>("Installer for this fix type is not implemented");
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

    public Result UninstallFix(
        GameEntity game,
        BaseFixEntity fix,
        string hostsFile = Consts.Hosts
        )
    {
        _logger.LogInformation($"Uninstalling {fix.Name} for {game.Name}");

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
                    Guard.IsNotNull(fileFix.InstalledFix);
                    _fileFixUninstaller.UninstallFix(game, fileFix.InstalledFix);
                    break;
                case RegistryFixEntity regFix:
                    Guard.IsNotNull(regFix.InstalledFix);
                    _registryFixUninstaller.UninstallFix(regFix.InstalledFix);
                    break;
                case RegistryFixV2Entity regFix2:
                    Guard.IsNotNull(regFix2.InstalledFix);
                    _registryFixUninstaller.UninstallFix(regFix2.InstalledFix);
                    break;
                case HostsFixEntity hostsFix:
                    Guard.IsNotNull(hostsFix.InstalledFix);
                    _hostsFixUninstaller.UninstallFix(hostsFix.InstalledFix, hostsFile);
                    break;
                default:
                    ThrowHelper.ThrowNotSupportedException("Uninstaller for this fix type is not implemented");
                    break;
            }
        }
        catch (IOException ex)
        {
            _logger.LogCritical(ex, "IO error while uninstalling fix");
            return new(ResultEnum.FileAccessError, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Error while uninstalling fix");
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

    public async Task<Result> UpdateFixAsync(
        GameEntity game,
        BaseFixEntity fix,
        string? variant,
        bool skipMD5Check,
        CancellationToken cancellationToken,
        string hostsFile = Consts.Hosts
        )
    {
        _logger.LogInformation($"Updating {fix.Name} for {game.Name}");

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
            case RegistryFixV2Entity registryFix:
                installedFix = _registryFixUpdater.UpdateFix(game, registryFix);
                break;
            case HostsFixEntity hostsFix:
                installedFix = _hostsFixUpdater.UpdateFix(game, hostsFix, hostsFile);
                break;
            default:
                return ThrowHelper.ThrowNotSupportedException<Result>("Updater for this fix type is not implemented");
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

