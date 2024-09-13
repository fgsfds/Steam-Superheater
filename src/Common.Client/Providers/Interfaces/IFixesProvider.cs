using Common.Entities.Fixes;
using Common.Entities.Fixes.FileFix;

namespace Common.Client.Providers.Interfaces;

public interface IFixesProvider
{
    /// <summary>
    /// Get fixes list from online or local repo
    /// </summary>
    Task<Result<List<FixesList>>> GetFixesListAsync();

    /// <summary>
    /// Get list of fixes sorted by dependency, with added game entities, and installed fixes
    /// </summary>
    Task<Result<List<FixesList>?>> GetPreparedFixesListAsync();

    /// <summary>
    /// Add or modify fix int the database
    /// </summary>
    /// <param name="gameId">Game id</param>
    /// <param name="gameName">Game name</param>
    /// <param name="fix">Fix</param>
    Task<Result> AddFixToDbAsync(int gameId, string gameName, BaseFixEntity fix);

    IEnumerable<FileFixEntity>? SharedFixes { get; }
}