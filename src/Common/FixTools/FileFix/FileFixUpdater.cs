using Common.Entities;
using Common.Entities.Fixes;
using Common.Entities.Fixes.FileFix;
using Common.Helpers;

namespace Common.FixTools.FileFix
{
    public sealed class FileFixUpdater(
        FileFixInstaller fixInstaller,
        FileFixUninstaller fixUninstaller
        )
    {
        private readonly FileFixInstaller _fixInstaller = fixInstaller;
        private readonly FileFixUninstaller _fixUninstaller = fixUninstaller;

        public async Task<BaseInstalledFixEntity> UpdateFixAsync(GameEntity game, FileFixEntity fix, string? variant, bool skipMD5Check, CancellationToken cancellationToken)
        {
            fix.InstalledFix.ThrowIfNull();

            _fixUninstaller.UninstallFix(game, fix.InstalledFix);

            var result = await _fixInstaller.InstallFixAsync(game, fix, variant, skipMD5Check, cancellationToken).ConfigureAwait(false);

            return result;
        }
    }
}
