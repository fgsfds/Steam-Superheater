using SteamFDCommon.Config;
using SteamFDTCommon.Entities;
using SteamFDTCommon.FixTools;
using SteamFDTCommon.Providers;
using System.Diagnostics;

namespace SteamFDCommon.Updater
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

        public async Task<bool> CheckForUpdates(Version currentVersion)
        {
            await CheckUpdater();

            _updates.Clear();

            _updates.AddRange(await GithubReleasesProvider.GetNewerReleasesListAsync(currentVersion));

            return _updates.Any();
        }

        private async Task<bool> CheckUpdater()
        {
            var fixes = await _fixesProvider.GetCachedFixesListAsync();
            var updater = fixes.Where(x => x.GameId == 0).First().Fixes.Where(x => x.Guid == Consts.UpdaterGuid).First();
            var currentVersion = _configProvider.Config.InstalledUpdater;

            if (!File.Exists("Updater.exe") ||
                !File.Exists("Updater.dll") ||
                updater.Version > currentVersion)
            {
                var result = await DownloadAndInstallUpdater(updater);
                return result;
            }

            return true;
        }

        private async Task<bool> DownloadAndInstallUpdater(FixEntity updater)
        {
            _ = await FixInstaller.InstallFix(new GameEntity(0, "", Directory.GetCurrentDirectory()), updater);

            _configProvider.Config.InstalledUpdater = updater.Version;

            return true;
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

        public void InstallUpdate()
        {
            Process.Start("Updater.exe", $"{Consts.ConfigFile};{Consts.InstalledFile}");
        }

        private void CreateUpdateLock(string fileName)
        {
            using (TextWriter tw = new StreamWriter(Consts.UpdateFile))
            {
                tw.WriteLine(fileName);
            }
        }
    }
}
