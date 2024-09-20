using Common.Entities;
using System.Collections.Immutable;

namespace Common.Client.Providers.Interfaces;

public interface IGamesProvider
{
    /// <summary>
    /// Get list of installed games
    /// </summary>
    /// <param name="dropCache">Drop current and create new cache</param>
    Task<ImmutableList<GameEntity>> GetGamesListAsync(bool dropCache);
}