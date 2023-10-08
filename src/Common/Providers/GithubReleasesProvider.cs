using Common.Entities;
using Common.Helpers;
using System.Text.Json;

namespace Common.Providers
{
    internal static class GitHubReleasesProvider
    {
        /// <summary>
        /// Return a list of releases from github repo that have higher version than the current
        /// </summary>
        /// <param name="currentVersion">current release version</param>
        public static async Task<IEnumerable<UpdateEntity>> GetNewerReleasesListAsync(Version currentVersion)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Superheater");

                var a = await client.GetStringAsync(Consts.GitHubReleases);

                var cc = JsonSerializer.Deserialize<List<GitHubRelease>>(a);

                var updates = cc.ConvertAll(x => new UpdateEntity(
                    new Version(x.tag_name),
                    x.body,
                    new Uri(x.assets.Where(x => x.name.EndsWith("win-x64.zip")).FirstOrDefault().browser_download_url)
                    ));

                var newVersions = updates.Where(x => x.Version > currentVersion);

                return newVersions;
            }
        }
    }
}
