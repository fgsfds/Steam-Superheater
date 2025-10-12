using Common.Client.Providers.Interfaces;
using Common.Entities.Fixes;
using Common.Entities.Fixes.FileFix;

namespace Common.Client.Providers.Fakes;

public sealed class FixesProviderFake : IFixesProvider
{
    public Task<Result> AddFixToDbAsync(int gameId, string gameName, BaseFixEntity fix)
    {
        return new(() => new(ResultEnum.Success, ""));
    }

    public ValueTask<Result<List<FixesList>>> GetFixesListAsync(bool localFixesOnly, bool dropCache)
    {
        return ValueTask.FromResult<Result<List<FixesList>>>(new(ResultEnum.Success, [], string.Empty));
    }

    public ValueTask<Result<List<FixesList>?>> GetPreparedFixesListAsync(bool localFixesOnly, bool dropFixesCache, bool dropGamesCache)
    {
        return ValueTask.FromResult<Result<List<FixesList>?>>(new(ResultEnum.Success, [], string.Empty));
    }

    public IEnumerable<FileFixEntity>? SharedFixes => [];

    public Dictionary<Guid, int>? Installs => [];

    public Dictionary<Guid, int>? Scores => [];
}
