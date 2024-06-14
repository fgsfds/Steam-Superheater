using Common.Entities;
using Common.Helpers;
using System.Text.Json;

namespace Superheater.Web.Server.Providers
{
    public sealed class AppReleasesProvider
    {
        private readonly ILogger<AppReleasesProvider> _logger;
        private readonly HttpClient _httpClient;

        public AppReleaseEntity? WindowsRelease { get; private set; }
        public AppReleaseEntity? LinuxRelease { get; private set; }

        public AppReleasesProvider(
            ILogger<AppReleasesProvider> logger,
            HttpClient httpClient)
        {
            _logger = logger;
            _httpClient = httpClient;
        }

        /// <summary>
        /// Return the latest new release or null if there's no newer releases
        /// </summary>
        public async Task GetLatestVersionAsync()
        {
            _logger.LogInformation("Looking for new releases");

            using var response = await _httpClient.GetAsync("https://api.github.com/repos/fgsfds/Steam-Superheater/releases", HttpCompletionOption.ResponseHeadersRead);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Error while getting releases" + Environment.NewLine + response.StatusCode);
                return;
            }

            var releasesJson = await response.Content.ReadAsStringAsync();

            var releases =
                JsonSerializer.Deserialize(releasesJson, GitHubReleaseEntityContext.Default.ListGitHubReleaseEntity)
                ?? ThrowHelper.Exception<List<GitHubReleaseEntity>>("Error while deserializing GitHub releases");

            releases = [.. releases.Where(static x => x.IsDraft is false && x.IsPrerelease is false).OrderByDescending(static x => new Version(x.TagName))];

            AppReleaseEntity? windowsRelease = null;
            AppReleaseEntity? linuxRelease = null;

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

        private AppReleaseEntity? GetRelease(GitHubReleaseEntity release, string osPostfix)
        {
            var asset = release.Assets.FirstOrDefault(x => x.FileName.EndsWith(osPostfix));

            if (asset is null)
            {
                return null;
            }

            var version = new Version(release.TagName);
            var description = release.Description;
            var downloadUrl = new Uri(asset.DownloadUrl);

            AppReleaseEntity update = new()
            {
                Version = version,
                Description = description,
                DownloadUrl = downloadUrl
            };

            return update;
        }
    }
}
