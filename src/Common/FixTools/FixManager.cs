using Common.Entities;
using Common.Entities.Fixes;
using Common.Entities.Fixes.FileFix;
using Common.FixTools.FileFix;
using Common.Helpers;

namespace Common.FixTools
{
    public class FixManager(
        FileFixInstaller fileFixInstaller,
        FileFixUninstaller fileFixUninstaller,
        FileFixUpdater fileFixUpdater
        )
    {
        private readonly FileFixInstaller _fileFixInstaller = fileFixInstaller ?? ThrowHelper.NullReferenceException<FileFixInstaller>(nameof(fileFixInstaller));
        private readonly FileFixUninstaller _fileFixUninstaller = fileFixUninstaller ?? ThrowHelper.NullReferenceException<FileFixUninstaller>(nameof(fileFixUninstaller));
        private readonly FileFixUpdater _fileFixUpdater = fileFixUpdater ?? ThrowHelper.NullReferenceException<FileFixUpdater>(nameof(fileFixUpdater));

        public async Task<BaseInstalledFixEntity> InstallFixAsync(GameEntity game, BaseFixEntity fix, string? variant, bool skipMD5Check)
        {
            if (fix is FileFixEntity fileFix)
            {
                return await _fileFixInstaller.InstallFixAsync(game, fileFix, variant, skipMD5Check);
            }

            return ThrowHelper.NotImplementedException<BaseInstalledFixEntity>("Installer for this fix type is not implemented");
        }

        public void UninstallFix(GameEntity game, BaseInstalledFixEntity installedFix, BaseFixEntity fix)
        {
            if (fix is FileFixEntity fileFix)
            {
                _fileFixUninstaller.UninstallFix(game, installedFix, fileFix);
            }
            else
            {
                ThrowHelper.NotImplementedException("Uninstaller for this fix type is not implemented");
            }
        }

        public async Task<BaseInstalledFixEntity> UpdateFixAsync(GameEntity game, BaseFixEntity fix, string? variant, bool skipMD5Check)
        {
            if (fix is FileFixEntity fileFix)
            {
                return await _fileFixUpdater.UpdateFixAsync(game, fileFix, variant, skipMD5Check);
            }

            return ThrowHelper.NotImplementedException<BaseInstalledFixEntity>("Updater for this fix type is not implemented");
        }
    }
}
