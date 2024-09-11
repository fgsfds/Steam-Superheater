using Api.Common.Interface;
using Common.Entities.Fixes;
using Common.Entities.Fixes.FileFix;
using Common.Enums;
using Common.Helpers;
using Database.Client;
using System.Text.Json;

namespace Common.Client.Providers;

public sealed class FixesProvider
{
    private readonly ApiInterface _apiInterface;
    private readonly GamesProvider _gamesProvider;
    private readonly InstalledFixesProvider _installedFixesProvider;
    private readonly DatabaseContextFactory _dbContextFactory;
    private readonly Logger _logger;

    public volatile IEnumerable<FileFixEntity>? SharedFixes;

    public FixesProvider(
        ApiInterface apiInterface,
        GamesProvider gamesProvider,
        InstalledFixesProvider installedFixesProvider,
        DatabaseContextFactory dbContextFactory,
        Logger logger
        )
    {
        _apiInterface = apiInterface;
        _gamesProvider = gamesProvider;
        _installedFixesProvider = installedFixesProvider;
        _dbContextFactory = dbContextFactory;
        _logger = logger;
    }

    /// <summary>
    /// Get fixes list from online or local repo
    /// </summary>
    public async Task<Result<List<FixesList>>> GetFixesListAsync()
    {
        using var dbContext = _dbContextFactory.Get();

        var fixesCacheDbEntity = dbContext.Cache.Find(DatabaseTableEnum.Fixes)!;
        var currentFixesVersion = fixesCacheDbEntity.Version!;
        var currentFixesList = JsonSerializer.Deserialize(fixesCacheDbEntity.Data, FixesListContext.Default.ListFixesList)!;

        var newFixesList = await _apiInterface.GetFixesListAsync(currentFixesVersion).ConfigureAwait(false);

        if (newFixesList.IsSuccess && newFixesList.ResultObject?.Version > currentFixesVersion)
        {
            foreach (var newGame in newFixesList.ResultObject.Fixes)
            {
                var existingGame = currentFixesList.FirstOrDefault(x => x.GameId == newGame.GameId);

                if (existingGame is null)
                {
                    currentFixesList.Add(newGame);
                    currentFixesList = [.. currentFixesList.OrderBy(static x => x.GameName)];
                }
                else
                {
                    foreach (var newFix in newGame.Fixes)
                    {
                        var existingFix = existingGame.Fixes.FirstOrDefault(x => x.Guid == newFix.Guid);

                        if (existingFix is null)
                        {
                            existingGame.Fixes.Add(newFix);
                            existingGame.Fixes = [.. existingGame.Fixes.OrderBy(static x => x.Name)];
                        }
                        else
                        {
                            var index = existingGame.Fixes.IndexOf(existingFix);
                            existingGame.Fixes[index] = newFix;
                        }
                    }
                }
            }

            fixesCacheDbEntity.Version = newFixesList.ResultObject.Version;
            fixesCacheDbEntity.Data = JsonSerializer.Serialize(currentFixesList, FixesListContext.Default.ListFixesList);

            _ = await dbContext.SaveChangesAsync().ConfigureAwait(false);
        }

        SharedFixes = currentFixesList.First(static x => x.GameId == 0).Fixes.Select(static x => x as FileFixEntity)!;

        return new(newFixesList.ResultEnum, currentFixesList, newFixesList.Message);
    }

    /// <summary>
    /// Get list of fixes sorted by dependency, with added game entities, and installed fixes
    /// </summary>
    public async Task<Result<List<FixesList>?>> GetPreparedFixesListAsync()
    {
        var fixesLists = await GetFixesListAsync().ConfigureAwait(false);

        if (fixesLists.ResultObject is null)
        {
            return new(fixesLists.ResultEnum, null, fixesLists.Message);
        }

        var games = await _gamesProvider.GetGamesListAsync().ConfigureAwait(false);
        var installedFixes = await _installedFixesProvider.GetInstalledFixesListAsync().ConfigureAwait(false);

        foreach (var fixesList in fixesLists.ResultObject)
        {
            if (fixesList.GameId == 0)
            {
                continue;
            }

            var woDependencies = fixesList.Fixes.Where(static x => x.Dependencies is null).ToList();
            var withDependencies = fixesList.Fixes.Except(woDependencies).OrderByDescending(static x => x.Dependencies!.Count).ToList();

            while (withDependencies.Count > 0)
            {
                foreach (var dep in withDependencies)
                {
                    var guid = dep.Dependencies![0];
                    var existing = woDependencies.FirstOrDefault(x => x.Guid == guid);

                    dep.DependencyLevel += dep.Dependencies.Count;

                    if (existing is null)
                    {
                        continue;
                    }

                    var oldIndex = fixesList.Fixes.IndexOf(dep);
                    fixesList.Fixes.RemoveAt(oldIndex);

                    var newIndex = fixesList.Fixes.IndexOf(existing) + 1;
                    fixesList.Fixes.Insert(newIndex, dep);

                    woDependencies.Add(dep);
                    _ = withDependencies.Remove(dep);

                    break;
                }
            }

            foreach (var fix in fixesList.Fixes)
            {
                var installed = installedFixes.FirstOrDefault(x => x.GameId == fixesList.GameId && x.Guid == fix.Guid);

                if (fix is FileFixEntity fileFix &&
                    fileFix.SharedFixGuid is not null)
                {
                    if (fixesLists.ResultObject.First(static x => x.GameId == 0).Fixes.First(x => x.Guid == fileFix.SharedFixGuid) is not FileFixEntity sharedFix)
                    {
                        return new(ResultEnum.Error, null, "Error while getting shared fix");
                    }

                    sharedFix.InstallFolder = fileFix.SharedFixInstallFolder;

                    if (installed is FileInstalledFixEntity fileInstalled)
                    {
                        sharedFix.InstalledFix = fileInstalled.InstalledSharedFix;
                    }

                    fileFix.SharedFix = sharedFix;
                }

                if (installed is not null)
                {
                    fix.InstalledFix = installed;
                }
            }

            var game = games.FirstOrDefault(x => x.Id == fixesList.GameId);
            fixesList.Game = game;
        }

        var result = fixesLists.ResultObject.OrderByDescending(static x => x.IsGameInstalled);

        return new(ResultEnum.Success, [.. result], string.Empty);
    }

    /// <summary>
    /// Add or modify fix int the database
    /// </summary>
    /// <param name="gameId">Game id</param>
    /// <param name="gameName">Game name</param>
    /// <param name="fix">Fix</param>
    public async Task<Result> AddFixToDbAsync(int gameId, string gameName, BaseFixEntity fix)
    {
        var fileFixResult = PrepareFixes(fix);

        if (fileFixResult != ResultEnum.Success)
        {
            return fileFixResult;
        }

        var result = await _apiInterface.AddFixToDbAsync(gameId, gameName, fix).ConfigureAwait(false);

        return result;
    }

    private Result PrepareFixes(BaseFixEntity fix)
    {
        if (fix is FileFixEntity fileFix)
        {
            var result = PrepareFileFixes(fileFix);

            if (!result.IsSuccess)
            {
                return result;
            }
        }

        if (string.IsNullOrWhiteSpace(fix.Description))
        {
            fix.Description = null;
        }
        if (string.IsNullOrWhiteSpace(fix.Notes))
        {
            fix.Notes = null;
        }
        if (fix.Dependencies?.Count == 0)
        {
            fix.Dependencies = null;
        }
        if (fix.Tags?.Any(static x => string.IsNullOrWhiteSpace(x)) ?? false)
        {
            fix.Tags = null;
        }

        return new Result(ResultEnum.Success, string.Empty);
    }

    private Result PrepareFileFixes(FileFixEntity fileFix)
    {
        if (!string.IsNullOrEmpty(fileFix.Url))
        {
            if (!fileFix.Url.StartsWith("http"))
            {
                fileFix.Url = Consts.FilesBucketUrl + fileFix.Url;
            }
        }

        if (string.IsNullOrWhiteSpace(fileFix.RunAfterInstall))
        {
            fileFix.RunAfterInstall = null;
        }
        if (string.IsNullOrWhiteSpace(fileFix.InstallFolder))
        {
            fileFix.InstallFolder = null;
        }
        if (string.IsNullOrWhiteSpace(fileFix.ConfigFile))
        {
            fileFix.ConfigFile = null;
        }
        if (fileFix.FilesToBackup?.Any(static x => string.IsNullOrWhiteSpace(x)) ?? false)
        {
            fileFix.FilesToBackup = null;
        }
        if (fileFix.FilesToDelete?.Any(static x => string.IsNullOrWhiteSpace(x)) ?? false)
        {
            fileFix.FilesToDelete = null;
        }
        if (fileFix.FilesToPatch?.Any(static x => string.IsNullOrWhiteSpace(x)) ?? false)
        {
            fileFix.FilesToPatch = null;
        }
        if (string.IsNullOrWhiteSpace(fileFix.SharedFixInstallFolder))
        {
            fileFix.SharedFixInstallFolder = null;
        }

        return new Result(ResultEnum.Success, string.Empty);
    }
}

