using Common.Client.Providers.Interfaces;
using Common.Entities;
using System.Collections.Immutable;

namespace Common.Client.Providers.Fakes;

public sealed class GamesProviderFake : IGamesProvider
{
    public Task<ImmutableList<GameEntity>> GetGamesListAsync(bool dropCache)
    {
        return new(() => []);
    }

    public Task UpdateCacheAsync()
    {
        return Task.CompletedTask;
    }
}
