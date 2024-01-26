using System.Text.Json;
using Common.Entities;
using Common.Helpers;

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
            Logger.Info("Requesting newer releases from GitHub");

            using (HttpClient client = new())
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Superheater");

                var a = await client.GetStringAsync(Consts.GitHubReleases);

                var releases = JsonSerializer.Deserialize(a, GitHubReleaseContext.Default.ListGitHubRelease)
                    ?? ThrowHelper.Exception<List<GitHubRelease>>("Error while deserializing GitHub releases");

                releases = [.. releases.Where(static x => x.draft is false && x.prerelease is false)];

                var updates = releases.ConvertAll(static x => new AppUpdateEntity()
                {
                    Version = new Version(x.tag_name),
                    Description = x.body,
                    DownloadUrl = new Uri(x.assets.First(static x => x.name.EndsWith("win-x64.zip")).browser_download_url)
                }
                );

                var newVersions = updates.Where(x => x.Version > currentVersion);

                return newVersions;
            }
        }
    }
}
