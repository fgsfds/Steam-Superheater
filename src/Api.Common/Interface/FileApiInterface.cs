using Api.Common.Messages;
using Common;
using Common.Entities;
using Common.Entities.Fixes;
using Common.Enums;
using Common.Helpers;
using Common.Providers;
using System.Text.Json;

namespace Api.Common.Interface;

public sealed class FileApiInterface : IApiInterface
{
    private readonly AppReleasesProvider _appReleasesProvider;
    private readonly HttpClient _httpClient;

    public FileApiInterface(
        AppReleasesProvider appReleasesProvider,
        HttpClient httpClient
        )
    {
        _appReleasesProvider = appReleasesProvider;
        _httpClient = httpClient;
    }

    public async Task<Result<GetFixesOutMessage?>> GetFixesListAsync(int tableVersion, Version appVersion)
    {
        try
        {
            var addons = await _httpClient.GetStringAsync(Consts.FixesJsonUrl).ConfigureAwait(false);

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
            var news = await _httpClient.GetStringAsync(Consts.NewsJsonUrl).ConfigureAwait(false);

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

    public Task<Result<string?>> GetSignedUrlAsync(string path)
    {
        var url = Consts.FilesBucketUrl + "uploads/" + path;

        return Task.FromResult(new Result<string?>(ResultEnum.Success, url, string.Empty));
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
}
