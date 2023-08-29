using Newtonsoft.Json;
using SteamFDCommon.Entities;

namespace SteamFDCommon.Providers
{
    public static class GithubReleasesProvider
    {
        /// <summary>
        /// Return a list of releases from github repo that have higher version than the current
        /// </summary>
        /// <param name="currentVersion">current release version</param>
        public static async Task<IEnumerable<UpdateEntity>> GetNewerReleasesListAsync(Version currentVersion)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd("SteamFD");

                var a = await client.GetStringAsync(Consts.GitHubReleases);

                var cc = JsonConvert.DeserializeObject<List<GithubReleaseEntity.Root>>(a);

                var updates = cc.ConvertAll(x => new UpdateEntity(
                    new Version(x.tag_name),
                    x.body,
                    new Uri(x.assets.First().browser_download_url)
                    ));

                var newVersions = updates.Where(x => x.Version > currentVersion);

                return newVersions;
            }
        }
    }
}
