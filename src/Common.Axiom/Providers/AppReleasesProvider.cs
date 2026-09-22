using System.Text.Json;
using Common.Axiom.Entities;
using Microsoft.Extensions.Logging;

namespace Common.Axiom.Providers;

public sealed class AppReleasesProvider
{
    private readonly ILogger _logger;
    private readonly HttpClient _httpClient;

    public AppReleaseEntity? WindowsRelease { get; private set; }
    public AppReleaseEntity? LinuxRelease { get; private set; }

    public AppReleasesProvider(
        ILogger logger,
        HttpClient httpClient
        )
    {
        _logger = logger;
        _httpClient = httpClient;
    }

    /// <summary>
    /// Look for the latest Windows and Linux app releases
    /// </summary>
    /// <returns>Result of the operation</returns>
    public async Task<Result> GetLatestVersionAsync()
    {
        _logger.LogInformation("Looking for new releases");

        try
        {
            using var response = await _httpClient.GetAsync("https://api.github.com/repos/fgsfds/Steam-Superheater/releases", HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Error while getting releases" + Environment.NewLine + response.StatusCode);
                return new(ResultEnum.ConnectionError, "Error while getting releases");
            }

            var releasesJson = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            var releases =
                JsonSerializer.Deserialize(releasesJson, GitHubReleaseEntityContext.Default.ListGitHubReleaseEntity)
                ?? throw new InvalidDataException("Error while deserializing GitHub releases");

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

            if (windowsRelease is null)
            {
                _logger.LogWarning("Windows release not found");
            }
            else
            {
                _logger.LogInformation($"Found Windows release {windowsRelease.Version}");
            }

            WindowsRelease = windowsRelease;

            if (linuxRelease is null)
            {
                _logger.LogWarning("Linux release not found");
            }
            else
            {
                _logger.LogInformation($"Found Linux release {linuxRelease.Version}");
            }

            LinuxRelease = linuxRelease;

            return new(ResultEnum.Success, string.Empty);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogError(ex, "GitHub is not responding");
            return new(ResultEnum.ConnectionError, "GitHub is not responding");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while getting releases");
            return new(ResultEnum.Error, "Error while getting releases");
        }
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

