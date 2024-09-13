using Common.Client.Providers.Interfaces;
using Common.Entities;
using Common.Entities.Fixes;
using System.Collections.Immutable;

namespace Common.Client.Providers.Fakes;

public sealed class InstalledFixesProviderFake : IInstalledFixesProvider
{
    public Task<ImmutableList<BaseInstalledFixEntity>> GetInstalledFixesListAsync()
    {
        return new(() => []);
    }

    public Result CreateInstalledJson(GameEntity game, BaseInstalledFixEntity installedFix)
    {
        return new(ResultEnum.Success, "");
    }

    public Result RemoveInstalledJson(GameEntity game, Guid fixGuid)
    {
        return new(ResultEnum.Success, "");
    }
}
