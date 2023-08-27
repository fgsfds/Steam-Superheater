using System.Diagnostics;

namespace SteamFDCommon.Updater
{
    public class UpdateInstaller
    {
        private readonly List<UpdateEntity> _updates;

        public UpdateInstaller()
        {
            _updates = new();
        }

        public async Task<bool> CheckForUpdates(Version currentVersion)
        {
            _updates.Clear();

            _updates.AddRange(await GithubReleasesProvider.GetNewerReleasesListAsync(currentVersion));

            return _updates.Any();
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
