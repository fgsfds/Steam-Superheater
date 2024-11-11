using Api.Common.Messages;
using Common;
using Common.Entities;
using Common.Entities.Fixes;
using Common.Enums;

namespace Api.Common.Interface;
public interface IApiInterface
{
    Task<Result> AddFixToDbAsync(int gameId, string gameName, BaseFixEntity fix);
    Task<Result> AddNewsAsync(string content);
    Task<Result<int?>> AddNumberOfInstallsAsync(Guid guid, Version appVersion);
    Task<Result> ChangeFixStateAsync(Guid guid, bool isDisabled);
    Task<Result> ChangeNewsAsync(DateTime date, string content);
    Task<Result<int?>> ChangeScoreAsync(Guid guid, sbyte increment);
    Task<Result<string?>> CheckIfFixExistsAsync(Guid guid);
    Task<Result<GetFixesOutMessage?>> GetFixesListAsync(int tableVersion, Version appVersion);
    Task<Result<GetFixesStatsOutMessage>> GetFixesStats();
    Task<Result<AppReleaseEntity?>> GetLatestAppReleaseAsync(OSEnum osEnum);
    Task<Result<GetNewsOutMessage>> GetNewsListAsync(int version);
    Task<Result<string?>> GetSignedUrlAsync(string path);
    Task<Result> ReportFixAsync(Guid guid, string text);
}