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

        public async Task<IInstalledFixEntity> InstallFixAsync(GameEntity game, IFixEntity fix, string? variant, bool skipMD5Check)
        {
            if (fix is FileFixEntity fileFix)
            {
                return await _fileFixInstaller.InstallFixAsync(game, fileFix, variant, skipMD5Check);
            }

            return ThrowHelper.NotImplementedException<IInstalledFixEntity>("");
        }

        public void UninstallFix(GameEntity game, IInstalledFixEntity installedFix, IFixEntity fix)
        {
            if (fix is FileFixEntity fileFix)
            {
                _fileFixUninstaller.UninstallFix(game, installedFix, fileFix);
            }

            ThrowHelper.NotImplementedException("");
        }

        public async Task<IInstalledFixEntity> UpdateFixAsync(GameEntity game, IFixEntity fix, string? variant, bool skipMD5Check)
        {
            if (fix is FileFixEntity fileFix)
            {
                return await _fileFixUpdater.UpdateFixAsync(game, fileFix, variant, skipMD5Check);
            }

            return ThrowHelper.NotImplementedException<IInstalledFixEntity>("");
        }
    }
}
