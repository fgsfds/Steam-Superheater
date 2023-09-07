using SteamFDCommon.Config;
using SteamFDCommon.Entities;
using SteamFDCommon.Providers;
using SteamFDCommon.FixTools;
using System.Diagnostics;
using SteamFDCommon.Helpers;

namespace SteamFDCommon
{
    public class UpdateInstaller
    {
        private readonly List<UpdateEntity> _updates;
        private readonly FixesProvider _fixesProvider;
        private readonly ConfigProvider _configProvider;

        public UpdateInstaller(
            FixesProvider fixesProvider,
            ConfigProvider configProvider
            )
        {
            _updates = new();
            _fixesProvider = fixesProvider ?? throw new NullReferenceException(nameof(fixesProvider));
            _configProvider = configProvider ?? throw new NullReferenceException(nameof(configProvider));
        }

        /// <summary>
        /// Download latest release from Github and create update lock file
        /// </summary>
        /// <returns></returns>
        public async Task DownloadLatestReleaseAndCreateLock()
        {
            var fixUrl = _updates.OrderByDescending(x => x.Version).First().DownloadUrl;

            var fileName = Path.GetFileName(fixUrl.ToString());

            await ZipTools.DownloadFileAsync(fixUrl, fileName);

            CreateUpdateLock(fileName);
        }

        /// <summary>
        /// Start updater
        /// </summary>
        public static void InstallUpdate()
        {
            Process.Start(Consts.UpdaterExe, $"{Consts.ConfigFile};{Consts.InstalledFile}");
        }

        /// <summary>
        /// First, check updater, then check if there are updates for SFD
        /// </summary>
        /// <param name="currentVersion">Current SFD verson</param>
        /// <returns></returns>
        public async Task<bool> CheckForUpdates(Version currentVersion)
        {
            await CheckUpdater();

            _updates.Clear();

            _updates.AddRange(await GithubReleasesProvider.GetNewerReleasesListAsync(currentVersion));

            return _updates.Any();
        }

        /// <summary>
        /// Check if updater exists and is up to date
        /// </summary>
        /// <returns></returns>
        private async Task<bool> CheckUpdater()
        {
            var fixes = await _fixesProvider.GetCachedFixesListAsync();
            var updater = fixes.Where(x => x.GameId == 0).First().Fixes.Where(x => x.Guid == CommonProperties.UpdaterGuid).First();
            var currentVersion = _configProvider.Config.InstalledUpdater;

            if (!File.Exists(Consts.UpdaterExe) ||
                !File.Exists("Updater.dll") ||
                updater.Version > currentVersion)
            {
                var result = await DownloadAndInstallUpdater(updater);
                return result;
            }

            return true;
        }

        /// <summary>
        /// Download and install the latest version of updater
        /// </summary>
        /// <param name="updater"></param>
        /// <returns></returns>
        private async Task<bool> DownloadAndInstallUpdater(FixEntity updater)
        {
            _ = await FixInstaller.InstallFix(new GameEntity(0, "", Directory.GetCurrentDirectory()), updater, false);

            _configProvider.Config.InstalledUpdater = updater.Version;

            return true;
        }

        /// <summary>
        /// Create update lock file
        /// </summary>
        /// <param name="fileName">update file</param>
        private void CreateUpdateLock(string fileName)
        {
            using (TextWriter tw = new StreamWriter(Consts.UpdateFile))
            {
                tw.WriteLine(fileName);
            }
        }
    }
}
