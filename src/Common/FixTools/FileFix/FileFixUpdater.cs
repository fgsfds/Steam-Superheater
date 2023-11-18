using Common.Entities;
using Common.Entities.Fixes;
using Common.Entities.Fixes.FileFix;
using Common.Helpers;

namespace Common.FixTools.FileFix
{
    public sealed class FileFixUpdater(
        FileFixInstaller fixInstaller
        //FileFixUninstaller fixUninstaller
        )
    {
        private readonly FileFixInstaller _fixInstaller = fixInstaller ?? ThrowHelper.ArgumentNullException<FileFixInstaller>(nameof(fixInstaller));
        //private readonly FileFixUninstaller _fixUninstaller = fixUninstaller ?? ThrowHelper.ArgumentNullException<FileFixUninstaller>(nameof(fixUninstaller));

        public async Task<BaseInstalledFixEntity> UpdateFixAsync(GameEntity game, FileFixEntity fix, string? variant, bool skipMD5Check)
        {
            FileFixUninstaller.UninstallFix(game, fix);

            var result = await _fixInstaller.InstallFixAsync(game, fix, variant, skipMD5Check);

            return result;
        }
    }
}
