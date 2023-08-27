using SteamFDCommon.DI;
using SteamFDCommon.Updater;
using SteamFDTCommon.Entities;
using SteamFDTCommon.Providers;
using System.Collections.ObjectModel;

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
    }
}
