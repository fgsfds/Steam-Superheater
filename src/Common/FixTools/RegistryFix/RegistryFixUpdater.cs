using Common.Entities.Fixes;
using Common.Entities.Fixes.FileFix;
using Common.Helpers;

namespace Common.FixTools.RegistryFix
{
    public sealed class RegistryFixUpdater(
        RegistryFixInstaller fixInstaller,
        RegistryFixUninstaller fixUninstaller
        )
    {
        private readonly RegistryFixInstaller _fixInstaller = fixInstaller ?? ThrowHelper.ArgumentNullException<RegistryFixInstaller>(nameof(fixInstaller));
        private readonly RegistryFixUninstaller _fixUninstaller = fixUninstaller ?? ThrowHelper.ArgumentNullException<RegistryFixUninstaller>(nameof(fixUninstaller));

        public async Task<BaseInstalledFixEntity> UpdateFixAsync()
        {
            return new FileInstalledFixEntity();
        }
    }
}
