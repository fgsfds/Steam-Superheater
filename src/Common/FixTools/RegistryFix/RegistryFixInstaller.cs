using Common.Config;
using Common.Entities.Fixes;
using Common.Entities.Fixes.FileFix;
using Common.Helpers;

namespace Common.FixTools.RegistryFix
{
    public sealed class RegistryFixInstaller(ConfigProvider config)
    {
        private readonly ConfigEntity _configEntity = config.Config ?? ThrowHelper.ArgumentNullException<ConfigEntity>(nameof(config));

        /// <summary>
        /// Install fix: download ZIP, backup and delete files if needed, run post install events
        /// </summary>
        public async Task<BaseInstalledFixEntity> InstallFixAsync()
        {
            return new FileInstalledFixEntity();
        }
    }
}
