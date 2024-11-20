using Api.Common.Interface;
using Common.Client.Providers.Interfaces;
using Common.Entities.Fixes;
using Common.Entities.Fixes.FileFix;
using Common.Enums;
using Common.Helpers;
using CommunityToolkit.Diagnostics;
using Database.Client;
using System.Text.Json;

namespace Common.Client.Providers;

public sealed class FixesProvider : IFixesProvider
{
    private readonly IApiInterface _apiInterface;
    private readonly IGamesProvider _gamesProvider;
    private readonly IInstalledFixesProvider _installedFixesProvider;
    private readonly DatabaseContextFactory _dbContextFactory;
    private readonly SemaphoreSlim _semaphore = new(1);

    private List<FixesList>? _cache;

    public IEnumerable<FileFixEntity>? SharedFixes { get; private set; }
    public Dictionary<Guid, int>? Installs { get; private set; }
    public Dictionary<Guid, int>? Scores { get; private set; }


    public FixesProvider(
        IApiInterface apiInterface,
        IGamesProvider gamesProvider,
        IInstalledFixesProvider installedFixesProvider,
        DatabaseContextFactory dbContextFactory
        )
    {
        _apiInterface = apiInterface;
        _gamesProvider = gamesProvider;
        _installedFixesProvider = installedFixesProvider;
        _dbContextFactory = dbContextFactory;
    }


    /// <inheritdoc/>
    public async Task<Result<List<FixesList>>> GetFixesListAsync(bool localFixesOnly, bool dropCache)
    {
        if (_cache is not null && !dropCache)
        {
            return new(ResultEnum.Success, _cache, string.Empty);
        }

        try
        {
            await _semaphore.WaitAsync().ConfigureAwait(false);

            var result = await UpdateCacheAsync(localFixesOnly).ConfigureAwait(false);

            Guard.IsNotNull(_cache);

            return new(result.ResultEnum, _cache, result.Message);
        }
        finally
        {
            _ = _semaphore.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<Result<List<FixesList>?>> GetPreparedFixesListAsync(bool localFixesOnly, bool dropFixesCache, bool dropGamesCache)
    {
        var fixesLists = await GetFixesListAsync(localFixesOnly, dropFixesCache).ConfigureAwait(false);

        Guard.IsNotNull(fixesLists.ResultObject);

        var games = await _gamesProvider.GetGamesListAsync(dropGamesCache).ConfigureAwait(false);
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

                    dep.DependencyLevel += 1;

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

        return new(fixesLists.ResultEnum, [.. result], fixesLists.Message);
    }

    /// <inheritdoc/>
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


    /// <summary>
    /// Update fixes cache
    /// </summary>
    /// <param name="localFixesOnly">Load only local cached fixes</param>
    private async Task<Result> UpdateCacheAsync(bool localFixesOnly)
    {
        using var dbContext = _dbContextFactory.Get();

        var fixesCacheDbEntity = dbContext.Cache.Find(DatabaseTableEnum.Fixes)!;

        List<FixesList> currentFixesList;

        try
        {
            currentFixesList = JsonSerializer.Deserialize(fixesCacheDbEntity.Data, FixesListContext.Default.ListFixesList)!;
        }
        catch
        {
            fixesCacheDbEntity.Version = 0;
            fixesCacheDbEntity.Data = "[]";
            _ = await dbContext.SaveChangesAsync().ConfigureAwait(false);

            currentFixesList = [];
        }

        var currentFixesVersion = fixesCacheDbEntity.Version!;

        var result = ResultEnum.Success;
        var message = string.Empty;

        Installs = null;
        Scores = null;

        if (ClientProperties.IsOfflineMode)
        {
            var newFixesList = File.ReadAllText(@"..\..\..\..\db\fixes.json");
            currentFixesList = JsonSerializer.Deserialize(newFixesList, FixesListContext.Default.ListFixesList)!;
        }
        else if (!localFixesOnly)
        {
            var newFixesList = await _apiInterface.GetFixesListAsync(
                currentFixesVersion,
                ClientProperties.CurrentVersion).ConfigureAwait(false);

            if (newFixesList.IsSuccess && newFixesList.ResultObject?.Version == 0)
            {
                currentFixesList = [];
                currentFixesVersion = -1;
            }

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
                                existingGame.Fixes = [.. existingGame.Fixes.OrderBy(static x => x.Name).ThenBy(x => x.Tags?.Contains("#nointro"))];
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

            var fixesStats = await _apiInterface.GetFixesStats().ConfigureAwait(false);

            if (fixesStats.IsSuccess)
            {
                Installs = fixesStats.ResultObject.Installs;
                Scores = fixesStats.ResultObject.Scores;
            }

            result = newFixesList.ResultEnum;
            message = newFixesList.Message;
        }

        SharedFixes = currentFixesList.FirstOrDefault(static x => x.GameId == 0)?.Fixes.Select(static x => x as FileFixEntity)!;

        _cache = currentFixesList;

        return new(result, message);
    }

    private Result PrepareFixes(BaseFixEntity fix)
    {
        if (string.IsNullOrEmpty(fix.Name) ||
            string.IsNullOrEmpty(fix.Version))
        {
            return new(ResultEnum.Error, "Name and Version are required to upload a fix.");
        }

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
        if (string.IsNullOrWhiteSpace(fix.Changelog))
        {
            fix.Changelog = null;
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
