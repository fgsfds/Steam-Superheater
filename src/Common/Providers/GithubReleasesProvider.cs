using Common.Entities;
using Common.Helpers;
using System.Text.Json;

namespace Common.Providers
{
    public static class GitHubReleasesProvider
    {
        /// <summary>
        /// Return a list of releases from GitHub repo that have higher version than the current
        /// </summary>
        /// <param name="currentVersion">current release version</param>
        public static async Task<IEnumerable<AppUpdateEntity>> GetNewerReleasesListAsync(Version currentVersion)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Superheater");

                var a = await client.GetStringAsync(Consts.GitHubReleases);

                var releases = JsonSerializer.Deserialize<List<GitHubRelease>>(a)
                ?? ThrowHelper.Exception<List<GitHubRelease>>("Error while deserializing GitHub releases");

                releases = releases.Where(x => x.draft is false && x.prerelease is false).ToList();

                var updates = releases.ConvertAll(x => new AppUpdateEntity(
                    new Version(x.tag_name),
                    x.body,
                    new Uri(x.assets.Where(x => x.name.EndsWith("win-x64.zip")).First().browser_download_url)
                    ));

                var newVersions = updates.Where(x => x.Version > currentVersion);

                return newVersions;
            }
        }
    }
}
