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

    public Task<Result<List<FixesList>>> GetFixesListAsync(bool dropCache)
    {
        return new(new(() => new(ResultEnum.Success, [], "")));
    }

    public Task<Result<List<FixesList>?>> GetPreparedFixesListAsync(bool dropCache)
    {
        return new(new(() => new(ResultEnum.Success, [], "")));
    }

    public IEnumerable<FileFixEntity>? SharedFixes => [];

    public Dictionary<Guid, int>? Installs => [];

    public Dictionary<Guid, int>? Scores => [];
}
