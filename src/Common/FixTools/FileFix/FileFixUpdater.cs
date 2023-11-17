using Common.Entities;
using Common.Entities.Fixes;
using Common.Helpers;

namespace Common.FixTools.FileFix
{
    public sealed class FileFixUpdater(
        FileFixInstaller fixInstaller,
        FileFixUninstaller fixUninstaller
        ) : IFixUpdater
    {
        private readonly FileFixInstaller _fixInstaller = fixInstaller ?? ThrowHelper.ArgumentNullException<FileFixInstaller>(nameof(fixInstaller));
        private readonly FileFixUninstaller _fixUninstaller = fixUninstaller ?? ThrowHelper.ArgumentNullException<FileFixUninstaller>(nameof(fixUninstaller));

        public async Task<BaseInstalledFixEntity> UpdateFixAsync(GameEntity game, BaseFixEntity fix, string? variant, bool skipMD5Check)
        {
            if (fix.InstalledFix is null) ThrowHelper.NullReferenceException(nameof(fix.InstalledFix));

            _fixUninstaller.UninstallFix(game, fix.InstalledFix, fix);

            var result = await _fixInstaller.InstallFixAsync(game, fix, variant, skipMD5Check);

            return result;
        }
    }
}
