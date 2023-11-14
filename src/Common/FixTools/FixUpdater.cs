using Common.Entities;
using Common.Helpers;

namespace Common.FixTools
{
    public sealed class FixUpdater
    {
        public FixUpdater(FixInstaller fixInstaller)
        {
            _fixInstaller = fixInstaller ?? ThrowHelper.ArgumentNullException<FixInstaller>(nameof(fixInstaller));
        }

        private readonly FixInstaller _fixInstaller;

        public async Task<InstalledFixEntity> UpdateFixAsync(GameEntity game, FixEntity fix, string? variant, bool skipMD5Check)
        {
            if (fix.InstalledFix is null) ThrowHelper.NullReferenceException(nameof(fix.InstalledFix));

            FixUninstaller.UninstallFix(game, fix.InstalledFix, fix);

            var result = await _fixInstaller.InstallFix(game, fix, variant, skipMD5Check);

            return result;
        }
    }
}
