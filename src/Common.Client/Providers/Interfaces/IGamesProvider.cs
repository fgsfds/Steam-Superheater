using Common.Entities;
using System.Collections.Immutable;

namespace Common.Client.Providers.Interfaces;

public interface IGamesProvider
{
    /// <summary>
    /// Get list of installed games
    /// </summary>
    Task<ImmutableList<GameEntity>> GetGamesListAsync();
}