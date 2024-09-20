using Common.Entities.Fixes;
using Common.Entities.Fixes.FileFix;

namespace Common.Client.Providers.Interfaces;

public interface IFixesProvider
{
    /// <summary>
    /// Get fixes list from online or local repo
    /// </summary>
    /// <param name="localFixesOnly"></param>
    /// <param name="dropCache">Drop current and create new cache</param>
    Task<Result<List<FixesList>>> GetFixesListAsync(bool localFixesOnly, bool dropCache);

    /// <summary>
    /// Get list of fixes sorted by dependency, with added game entities, and installed fixes
    /// </summary>
    /// <param name="localFixesOnly"></param>
    /// <param name="dropFixesCache">Drop current and create new cache</param>
    /// <param name="dropGamesCache"></param>
    Task<Result<List<FixesList>?>> GetPreparedFixesListAsync(bool localFixesOnly, bool dropFixesCache, bool dropGamesCache);

    /// <summary>
    /// Add or modify fix int the database
    /// </summary>
    /// <param name="gameId">Game id</param>
    /// <param name="gameName">Game name</param>
    /// <param name="fix">Fix</param>
    Task<Result> AddFixToDbAsync(int gameId, string gameName, BaseFixEntity fix);

    IEnumerable<FileFixEntity>? SharedFixes { get; }
    Dictionary<Guid, int>? Installs { get; }
    Dictionary<Guid, int>? Scores { get; }
}