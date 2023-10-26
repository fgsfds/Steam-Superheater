using Common.Entities;

namespace Common.FixTools
{
    public sealed class FixUpdater
    {
        public FixUpdater(FixInstaller fixInstaller)
        {
            _fixInstaller = fixInstaller ?? throw new NullReferenceException(nameof(fixInstaller));
        }

        private readonly FixInstaller _fixInstaller;

        public async Task<InstalledFixEntity> UpdateFixAsync(GameEntity game, FixEntity fix, string? variant, bool skipMD5Check)
        {
            if (fix.InstalledFix is null) throw new NullReferenceException(nameof(fix.InstalledFix));

            FixUninstaller.UninstallFix(game, fix.InstalledFix, fix);

            var result = await _fixInstaller.InstallFix(game, fix, variant, skipMD5Check);

            return result;
        }
    }
}
