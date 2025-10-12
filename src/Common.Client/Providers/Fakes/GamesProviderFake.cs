using Common.Client.Providers.Interfaces;
using Common.Entities;
using System.Collections.Immutable;

namespace Common.Client.Providers.Fakes;

public sealed class GamesProviderFake : IGamesProvider
{
    public ValueTask<ImmutableList<GameEntity>> GetGamesListAsync(bool dropCache)
    {
        return ValueTask.FromResult<ImmutableList<GameEntity>>([]);
    }

    public Task UpdateCacheAsync()
    {
        return Task.CompletedTask;
    }
}
