using Common;
using Common.Entities;
using Common.Helpers;
using System.Text.Json;

namespace Superheater.Web.Server.Providers
{
    public sealed class AppReleasesProvider
    {
        private readonly ILogger<AppReleasesProvider> _logger;
        private readonly HttpClientInstance _httpClient;

        public AppUpdateEntity WindowsRelease { get; private set; }
        public AppUpdateEntity LinuxRelease { get; private set; }

        public AppReleasesProvider(
            ILogger<AppReleasesProvider> logger,
            HttpClientInstance httpClient)
        {
            _logger = logger;
            _httpClient = httpClient;
        }

        /// <summary>
        /// Return the latest new release or null if there's no newer releases
        /// </summary>
        public async Task GetLatestVersionAsync()
        {
            var releasesJson = await _httpClient.GetStringAsync(Consts.GitHubReleases).ConfigureAwait(false);

            _logger.LogInformation("Requesting newer releases from GitHub");

            var releases =
                JsonSerializer.Deserialize(releasesJson, GitHubReleaseContext.Default.ListGitHubRelease)
                ?? ThrowHelper.Exception<List<GitHubRelease>>("Error while deserializing GitHub releases");

            releases = [.. releases.Where(static x => x.draft is false && x.prerelease is false).OrderByDescending(static x => new Version(x.tag_name))];

            AppUpdateEntity? windowsRelease = null;
            AppUpdateEntity? linuxRelease = null;

            foreach (var release in releases)
            {
                windowsRelease = GetRelease(release, "win-x64.zip");

                if (windowsRelease is not null)
                {
                    break;
                }
            }

            foreach (var release in releases)
            {
                linuxRelease = GetRelease(release, "linux-x64.zip");

                if (linuxRelease is not null)
                {
                    break;
                }
            }

            WindowsRelease = windowsRelease!;
            LinuxRelease = linuxRelease!;
        }

        private AppUpdateEntity? GetRelease(GitHubRelease release, string osPostfix)
        {
            var asset = release.assets.FirstOrDefault(x => x.name.EndsWith(osPostfix));

            if (asset is null)
            {
                return null;
            }

            var version = new Version(release.tag_name);
            var description = release.body;
            var downloadUrl = new Uri(asset.browser_download_url);

            AppUpdateEntity update = new()
            {
                Version = version,
                Description = description,
                DownloadUrl = downloadUrl
            };

            return update;
        }
    }
}
