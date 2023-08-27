using SteamFDCommon.Updater;
using System.IO;

namespace SteamFDCommon.Models
{
    public class AboutModel
    {
        private readonly List<UpdateEntity> _updates;

        public AboutModel()
        {
            _updates = new();
        }

        /// <summary>
        /// Fill the list of newer releases and return true if there are newer versions
        /// </summary>
        /// <param name="currentVersion">Current app version</param>
        /// <returns>true if there are newer versions</returns>
        public async Task<bool> CheckForUpdates(Version currentVersion)
        {
            _updates.Clear();

            _updates.AddRange(await GithubReleasesProvider.GetNewerReleasesListAsync(currentVersion));

            return _updates.Any();
        }

        public async Task DownloadLatestReleaseAndCreateLock()
        {
            var fixUrl = _updates.OrderByDescending(x => x.Version).First().DownloadUrl;

            var fileName = Path.GetFileName(fixUrl.ToString());

            await ZipTools.DownloadFileAsync(fixUrl, fileName);

            CreateUpdateLock(fileName);
        }

        private void CreateUpdateLock(string fileName)
        {
            var updateFile = ".update";

            using (TextWriter tw = new StreamWriter(updateFile))
            {
                tw.WriteLine(fileName);
            }
        }
    }
}
