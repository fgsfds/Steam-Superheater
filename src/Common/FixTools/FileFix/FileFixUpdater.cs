using Common.Entities;
using Common.Entities.Fixes;
using Common.Entities.Fixes.FileFix;

namespace Common.FixTools.FileFix
{
    public sealed class FileFixUpdater(
        FileFixInstaller _fixInstaller,
        FileFixUninstaller _fixUninstaller
        )
    {

        public async Task<BaseInstalledFixEntity> UpdateFixAsync(GameEntity game, FileFixEntity fix, string? variant, bool skipMD5Check)
        {
            _fixUninstaller.UninstallFix(game, fix);

            var result = await _fixInstaller.InstallFixAsync(game, fix, variant, skipMD5Check);

            return result;
        }
    }
}
