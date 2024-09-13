using Common.Entities;
using Common.Entities.Fixes;
using System.Collections.Immutable;

namespace Common.Client.Providers.Interfaces;

public interface IInstalledFixesProvider
{
    /// <summary>
    /// Add installed fix to cache and create json
    /// </summary>
    /// <param name="game">Game</param>
    /// <param name="installedFix">Installed fix entity</param>
    Result CreateInstalledJson(GameEntity game, BaseInstalledFixEntity installedFix);

    Task<ImmutableList<BaseInstalledFixEntity>> GetInstalledFixesListAsync();

    /// <summary>
    /// Remove installed fix from cache and disk
    /// </summary>
    /// <param name="game">Game</param>
    /// <param name="fixGuid">Fix guid</param>
    Result RemoveInstalledJson(GameEntity game, Guid fixGuid);
}