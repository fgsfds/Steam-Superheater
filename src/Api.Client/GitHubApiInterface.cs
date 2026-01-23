using System.Text.Json;
using Api.Axiom.Interfaces;
using Api.Axiom.Messages;
using Common.Axiom;
using Common.Axiom.Entities;
using Common.Axiom.Entities.Fixes;
using Common.Axiom.Enums;
using Common.Axiom.Helpers;
using Common.Axiom.Providers;
using Common.Client;
using Microsoft.Extensions.Logging;

namespace Api.Client;

public sealed class GitHubApiInterface : IApiInterface
{
    private readonly AppReleasesProvider _appReleasesProvider;
    private readonly HttpClient _httpClient;
    private readonly SemaphoreSlim _semaphore = new(1);
    private readonly ILogger _logger;

    private Dictionary<string, string>? _data;

    public GitHubApiInterface(
        AppReleasesProvider appReleasesProvider,
        HttpClient httpClient,
        ILogger logger
        )
    {
        _appReleasesProvider = appReleasesProvider;
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<Result<GetFixesOutMessage?>> GetFixesListAsync(int tableVersion, Version appVersion)
    {
        try
        {
            var addons = await _httpClient.GetStringAsync(CommonConstants.FixesJsonUrl).ConfigureAwait(false);

            if (addons is null)
            {
                return new(ResultEnum.ConnectionError, null, "Error while getting fixes");
            }

            var addonsJson = JsonSerializer.Deserialize(addons, FixesListContext.Default.ListFixesList);

            if (addonsJson is null)
            {
                return new(ResultEnum.ConnectionError, null, "Error while getting fixes");
            }

            GetFixesOutMessage result = new()
            {
                Version = 0,
                Fixes = addonsJson
            };

            return new(ResultEnum.Success, result, string.Empty);
        }
        catch
        {
            return new(ResultEnum.ConnectionError, null, "Error while getting fixes");
        }
    }

    public async Task<Result<GetNewsOutMessage>> GetNewsListAsync(int version)
    {
        try
        {
            var news = await _httpClient.GetStringAsync(CommonConstants.NewsJsonUrl).ConfigureAwait(false);

            if (news is null)
            {
                return new(ResultEnum.ConnectionError, null, "Error while getting news");
            }

            var newsJson = JsonSerializer.Deserialize(news, NewsListEntityContext.Default.ListNewsEntity);

            if (newsJson is null)
            {
                return new(ResultEnum.ConnectionError, null, "Error while getting news");
            }

            GetNewsOutMessage result = new()
            {
                Version = 0,
                News = newsJson
            };

            return new(ResultEnum.Success, result, string.Empty);
        }
        catch
        {
            return new(ResultEnum.ConnectionError, null, "Error while getting news");
        }
    }

    public async Task<Result<AppReleaseEntity?>> GetLatestAppReleaseAsync(OSEnum osEnum)
    {
        try
        {
            await _appReleasesProvider.GetLatestVersionAsync().ConfigureAwait(false);

            return osEnum switch
            {
                OSEnum.Windows => new(ResultEnum.Success, _appReleasesProvider.WindowsRelease, string.Empty),
                OSEnum.Linux => new(ResultEnum.Success, _appReleasesProvider.LinuxRelease, string.Empty),
                _ => throw new NotImplementedException()
            };
        }
        catch
        {
            return new(ResultEnum.ConnectionError, null, "Error while getting latest app release");
        }
    }

    public async Task<Result<string?>> GetSignedUrlAsync(string path)
    {
        await _semaphore.WaitAsync().ConfigureAwait(false);

        try
        {
            if (_data is null)
            {
                await InitData().ConfigureAwait(false);
            }

            _ = _data!.TryGetValue(DataJson.UploadFolder, out var uploadFolder) ? uploadFolder : null;

            var url = uploadFolder + path;

            return new Result<string?>(ResultEnum.Success, url, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "=== Error while getting upload folder from GitHub ===");
            return new Result<string?>(ResultEnum.Error, null, "Error while getting upload folder from GitHub");
        }
        finally
        {
            _ = _semaphore.Release();
        }
    }


    public Task<Result> AddFixToDbAsync(int gameId, string gameName, BaseFixEntity fix) => Task.FromResult(new Result(ResultEnum.Error, string.Empty));

    public Task<Result> AddNewsAsync(string content) => Task.FromResult(new Result(ResultEnum.Error, string.Empty));

    public Task<Result<int?>> AddNumberOfInstallsAsync(Guid guid, Version appVersion) => Task.FromResult(new Result<int?>(ResultEnum.Error, null, string.Empty));

    public Task<Result> ChangeFixStateAsync(Guid guid, bool isDisabled) => Task.FromResult(new Result(ResultEnum.Error, string.Empty));

    public Task<Result> ChangeNewsAsync(DateTime date, string content) => Task.FromResult(new Result(ResultEnum.Error, string.Empty));

    public Task<Result<int?>> ChangeScoreAsync(Guid guid, sbyte increment) => Task.FromResult(new Result<int?>(ResultEnum.Error, null, string.Empty));

    public Task<Result<string?>> CheckIfFixExistsAsync(Guid guid) => Task.FromResult(new Result<string?>(ResultEnum.Error, null, string.Empty));

    public Task<Result<GetFixesStatsOutMessage>> GetFixesStats() => Task.FromResult(new Result<GetFixesStatsOutMessage>(ResultEnum.Error, null, string.Empty));

    public Task<Result> ReportFixAsync(Guid guid, string text) => Task.FromResult(new Result(ResultEnum.Error, string.Empty));

    private async Task InitData()
    {
        string data;

        if (ClientProperties.IsOfflineMode)
        {
            data = File.ReadAllText(Path.Combine("..", "..", "..", "..", "db", "fixes.json"));
        }
        else
        {
            //using var httpClient = _httpClientFactory.CreateClient(HttpClientEnum.GitHub.GetDescription());
            using var response = await _httpClient.GetAsync(CommonConstants.DataJsonUrl, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
            _ = response.EnsureSuccessStatusCode();

            data = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        }

        _data = JsonSerializer.Deserialize(data, DataJsonModelContext.Default.DictionaryStringString);

        if (_data is null)
        {
            throw new ArgumentNullException();
        }
    }
}
