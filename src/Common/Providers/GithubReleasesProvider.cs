using System.Runtime.InteropServices;
using System.Text.Json;
using Common.Entities;
using Common.Helpers;

namespace Common.Providers
{
    public static class GitHubReleasesProvider
    {
        /// <summary>
        /// Return the latest new release or null if there's not newer releases
        /// </summary>
        /// <param name="currentVersion">current release version</param>
        public static async Task<AppUpdateEntity?> GetLatestUpdateAsync(Version currentVersion)
        {
            Logger.Info("Requesting newer releases from GitHub");

            using (HttpClient client = new())
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Superheater");

                var a = await client.GetStringAsync(Consts.GitHubReleases);

                var releases = JsonSerializer.Deserialize(a, GitHubReleaseContext.Default.ListGitHubRelease)
                    ?? ThrowHelper.Exception<List<GitHubRelease>>("Error while deserializing GitHub releases");

                releases = [.. releases.Where(static x => x.draft is false && x.prerelease is false)];

                string osPostfix = string.Empty;

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    osPostfix = "win-x64.zip";
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    osPostfix = "linux-x64.zip";
                }
                else
                {
                    ThrowHelper.PlatformNotSupportedException();
                }

                AppUpdateEntity? update = null;

                foreach (var release in releases)
                {
                    var asset = release.assets.FirstOrDefault(x => x.name.EndsWith(osPostfix));

                    if (asset is null)
                    {
                        continue;
                    }

                    var version = new Version(release.tag_name);

                    if (version <= currentVersion ||
                        version < update?.Version)
                    {
                        continue;
                    }

                    var description = release.body;
                    var downloadUrl = new Uri(asset.browser_download_url);

                    update = new()
                    {
                        Version = version,
                        Description = description,
                        DownloadUrl = downloadUrl
                    };
                }

                return update;
            }
        }
    }
}
