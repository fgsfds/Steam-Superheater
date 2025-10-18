using System.Collections.Immutable;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using Api.Common.Interface;
using Common.Client.FilesTools;
using Common.Client.Providers.Interfaces;
using Common.Entities;
using Common.Entities.Fixes;
using Common.Entities.Fixes.FileFix;
using Common.Entities.Fixes.HostsFix;
using Common.Entities.Fixes.RegistryFix;
using Common.Entities.Fixes.TextFix;
using Common.Enums;
using Common.Helpers;
using CommunityToolkit.Diagnostics;

namespace Common.Client.Models;

public sealed class EditorModel
{
    private readonly IFixesProvider _fixesProvider;
    private readonly IGamesProvider _gamesProvider;
    private readonly FilesUploader _filesUploader;
    private readonly IApiInterface _apiInterface;
    private readonly HttpClient _httpClient;

    private List<FixesList> _fixesList = [];
    private List<GameEntity> _availableGamesList = [];

    /// <summary>
    /// List of games that can be added
    /// </summary>
    public ImmutableList<GameEntity> AvailableGames => [.. _availableGamesList];


    public EditorModel(
        IFixesProvider fixesProvider,
        IGamesProvider gamesProvider,
        FilesUploader filesUploader,
        IApiInterface apiInterface,
        HttpClient httpClient
        )
    {
        _fixesProvider = fixesProvider;
        _gamesProvider = gamesProvider;
        _filesUploader = filesUploader;
        _apiInterface = apiInterface;
        _httpClient = httpClient;
    }


    /// <summary>
    /// Update list of fixes from online or local repo
    /// </summary>
    /// <param name="dropCache">Drop current and create new cache</param>
    public async Task<Result> UpdateListsAsync(bool dropCache)
    {
        var result = await _fixesProvider.GetFixesListAsync(false, dropCache).ConfigureAwait(false);

        if (result.ResultObject is null)
        {
            return new(result.ResultEnum, result.Message);
        }

        _fixesList = result.ResultObject;

        await UpdateListOfAvailableGamesAsync(dropCache).ConfigureAwait(false);

        return new(result.ResultEnum, result.Message);
    }

    /// <summary>
    /// Get list of fixes optionally filtered by a search string
    /// </summary>
    /// <param name="search">Search string</param>
    /// <param name="tag">Selected tag</param>
    public ImmutableList<FixesList> GetFilteredGamesList(
        string? search = null,
        string? tag = null
        )
    {
        foreach (var entity in _fixesList)
        {
            foreach (var fix in entity.Fixes)
            {
                fix.IsHidden = false;
            }
        }

        if (string.IsNullOrEmpty(search) &&
            string.IsNullOrEmpty(tag))
        {
            return [.. _fixesList];
        }

        if (!string.IsNullOrEmpty(tag))
        {
            if (!tag.Equals(Consts.All))
            {
                foreach (var entity in _fixesList)
                {
                    foreach (var fix in entity.Fixes)
                    {
                        if (tag.Equals(Consts.WindowsOnly) && fix.SupportedOSes != OSEnum.Windows)
                        {
                            fix.IsHidden = true;
                        }
                        else if (tag.Equals(Consts.LinuxOnly) && fix.SupportedOSes != OSEnum.Linux)
                        {
                            fix.IsHidden = true;
                        }
                        else if (tag.Equals(Consts.AllSupported) && fix.SupportedOSes != (OSEnum.Linux | OSEnum.Windows))
                        {
                            fix.IsHidden = true;
                        }
                    }
                }
            }
        }

        if (search is null)
        {
            return [.. _fixesList.Where(static x => !x.IsEmpty)];
        }

        return [.. _fixesList.Where(x => !x.IsEmpty && x.GameName.Contains(search.Trim(), StringComparison.OrdinalIgnoreCase))];
    }

    /// <summary>
    /// Drop current fixes cache
    /// </summary>
    public void DropFixesList()
    {
        _fixesList = [];
    }

    /// <summary>
    /// Add new game with empty fix
    /// </summary>
    /// <param name="game">Game entity</param>
    /// <returns>New fixes list</returns>
    public FixesList AddNewGame(GameEntity game)
    {
        FixesList newFix = new()
        {
            GameId = game.Id,
            GameName = game.Name,
            Fixes = [new FileFixEntity(true)]
        };

        _fixesList.Add(newFix);

        _ = _availableGamesList.Remove(game);

        return newFix;
    }

    /// <summary>
    /// Add new fix for a game
    /// </summary>
    /// <param name="game">Game entity</param>
    /// <returns>New fix entity</returns>
    public FileFixEntity AddNewFix(FixesList game)
    {
        FileFixEntity newFix = new(true);

        game.Fixes.Add(newFix);

        return newFix;
    }

    /// <summary>
    /// Remove fix from a game
    /// </summary>
    /// <param name="fix">Fix entity</param>
    /// <param name="isDisabled">Is fix disabled</param>
    public async Task<Result> ChangeFixDisabledState(
        BaseFixEntity fix,
        bool isDisabled
        )
    {
        var result = await _apiInterface.ChangeFixStateAsync(fix.Guid, isDisabled).ConfigureAwait(false);

        if (result.IsSuccess)
        {
            fix.IsDisabled = isDisabled;
        }

        return result;
    }

    /// <summary>
    /// Get list of dependencies for a fix
    /// </summary>
    /// <param name="fixesList">Fixes list</param>
    /// <param name="fixEntity">Fix</param>
    /// <returns>List of dependencies</returns>
    public ImmutableList<BaseFixEntity>? GetDependenciesForAFix(
        FixesList? fixesList,
        BaseFixEntity? fixEntity
        )
    {
        if (fixEntity?.Dependencies is null ||
            fixesList is null)
        {
            return null;
        }

        var allGameDeps = fixEntity.Dependencies;

        var deps = fixesList.Fixes.Where(x => allGameDeps.Contains(x.Guid));

        return [.. deps];
    }

    /// <summary>
    /// Get list of fixes that can be added as dependencies
    /// </summary>
    /// <param name="fixesList">Fixes list</param>
    /// <param name="fixEntity">Fix</param>
    /// <returns>List of fixes</returns>
    public ImmutableList<BaseFixEntity>? GetListOfAvailableDependencies(
        FixesList? fixesList,
        BaseFixEntity? fixEntity
        )
    {
        if (fixesList is null ||
            fixEntity is null)
        {
            return null;
        }

        List<BaseFixEntity> result = [];

        var fixDependencies = GetDependenciesForAFix(fixesList, fixEntity);

        foreach (var fix in fixesList.Fixes)
        {
            if (fix.Guid == fixEntity.Guid)
            {
                //don't add itself
                continue;
            }

            if (fix.Dependencies is not null && fix.Dependencies.Contains(fixEntity.Guid))
            {
                //don't add fixes that depend on it
                continue;
            }

            if (fixDependencies is not null && fixDependencies.Exists(x => x.Guid == fix.Guid))
            {
                //don't add fixes that are already dependencies
                continue;
            }

            result.Add(fix);
        }

        return [.. result];
    }

    /// <summary>
    /// Upload fix to ftp
    /// </summary>
    /// <param name="fixesList">Fixes list entity</param>
    /// <param name="fix">New fix</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>true if uploaded successfully</returns>
    public async Task<Result> UploadFixAsync(
        FixesList fixesList,
        BaseFixEntity fix,
        CancellationToken cancellationToken
        )
    {
        var fixFilePath = CreateFixJson(fixesList, fix, false, out _, out _);

        List<string> filesToUpload = [fixFilePath];

        if (fix is FileFixEntity fileFix)
        {
            var url = fileFix.Url;

            if (!string.IsNullOrEmpty(url) &&
                !url.StartsWith("http"))
            {
                filesToUpload.Add(url);
            }
        }

        var result = await _filesUploader.UploadFilesAsync(fix.Guid.ToString(), filesToUpload, cancellationToken).ConfigureAwait(false);

        File.Delete(fixFilePath);

        return result;
    }

    public string CreateFixJson(
        FixesList fixesList,
        BaseFixEntity fix,
        bool isTestFix,
        out string newFixJsonString,
        out FixesList newFixesList
        )
    {
        if (isTestFix)
        {
            fix.IsTestFix = true;
            fix.IsDisabled = false;
        }

        newFixesList = new FixesList()
        {
            GameId = fixesList.GameId,
            GameName = fixesList.GameName,
            Fixes = [fix]
        };

        newFixJsonString = JsonSerializer.Serialize(newFixesList, SourceEntityContext.Default.FixesList);

        var fixFilePath = Path.Combine(ClientProperties.WorkingFolder, "fix.json");

        File.WriteAllText(fixFilePath, newFixJsonString);

        return fixFilePath;
    }

    /// <summary>
    /// Check if the file can be uploaded
    /// </summary>
    public async Task<Result> CheckFixBeforeUploadAsync(BaseFixEntity fix)
    {
        var doesFixExists = await _apiInterface.CheckIfFixExistsAsync(fix.Guid).ConfigureAwait(false);

        if (doesFixExists.IsSuccess && doesFixExists.ResultObject is not null)
        {
            return new(
                ResultEnum.Error,
                """
                    Can't upload fix.
                    
                    This fix already exists in the database.
                    """);
        }

        if (string.IsNullOrEmpty(fix.Name) ||
            string.IsNullOrEmpty(fix.Version))
        {
            return new(ResultEnum.Error, "Name and Version are required to upload a fix.");
        }

        if (fix is FileFixEntity fileFix &&
            !string.IsNullOrEmpty(fileFix.Url) &&
            !fileFix.Url.StartsWith("http"))
        {
            if (!File.Exists(fileFix.Url))
            {
                return new(ResultEnum.Error, $"{fileFix.Url} doesn't exist.");
            }

            if (new FileInfo(fileFix.Url).Length > 1e+9)
            {
                return new(
                    ResultEnum.Error,
                    """
                        Can't upload file larger than 1Gb.
                        
                        Please, upload it to file hosting.
                        """);
            }
        }

        return new(ResultEnum.Success, string.Empty);
    }

    public void AddDependencyForFix(
        BaseFixEntity addTo,
        BaseFixEntity dependency
        )
    {
        addTo.Dependencies ??= [];
        addTo.Dependencies.Add(dependency.Guid);
    }

    public void RemoveDependencyForFix(
        BaseFixEntity addTo,
        BaseFixEntity dependency
        )
    {
        Guard.IsNotNull(addTo.Dependencies);

        _ = addTo.Dependencies.Remove(dependency.Guid);

        if (addTo.Dependencies.Count == 0)
        {
            addTo.Dependencies = null;
        }
    }

    /// <summary>
    /// Change type of the fix
    /// </summary>
    /// <typeparam name="T">New fix type</typeparam>
    /// <param name="fixesList">List of fixes</param>
    /// <param name="fix">Fix to replace</param>
    public void ChangeFixType<T>(
        List<BaseFixEntity> fixesList,
        BaseFixEntity fix
        ) where T : BaseFixEntity
    {
        var fixIndex = fixesList.IndexOf(fix);

        if (typeof(T) == typeof(RegistryFixEntity))
        {
            RegistryFixEntity newFix = new(fix);
            fixesList[fixIndex] = newFix;
        }
        else if (typeof(T) == typeof(FileFixEntity))
        {
            FileFixEntity newFix = new(fix);
            fixesList[fixIndex] = newFix;
        }
        else if (typeof(T) == typeof(HostsFixEntity))
        {
            HostsFixEntity newFix = new(fix);
            fixesList[fixIndex] = newFix;
        }
        else if (typeof(T) == typeof(TextFixEntity))
        {
            TextFixEntity newFix = new(fix);
            fixesList[fixIndex] = newFix;
        }
        else
        {
            ThrowHelper.ThrowArgumentException(nameof(fix));
        }
    }

    /// <summary>
    /// Create or update list of games that can be added to the fixes list
    /// </summary>
    /// <param name="dropCache">Drop current and create new cache</param>
    private async Task UpdateListOfAvailableGamesAsync(bool dropCache)
    {
        var installedGames = await _gamesProvider.GetGamesListAsync(dropCache).ConfigureAwait(false);

        _availableGamesList = new(installedGames.Count);

        foreach (var game in installedGames)
        {
            if (!_fixesList.Exists(x => x.GameId == game.Id))
            {
                _availableGamesList.Add(game);
            }
        }
    }

    /// <summary>
    /// Add new fix from a fix json
    /// </summary>
    /// <param name="pathToFile">Path to json</param>
    public Result<Tuple<int, Guid>?> AddFixFromFile(string pathToFile)
    {
        try
        {
            var json = File.ReadAllText(pathToFile);

            var newFix = JsonSerializer.Deserialize(json, SourceEntityContext.Default.FixesList)!;

            var existingGame = _fixesList.FirstOrDefault(x => x.GameId == newFix.GameId);

            if (existingGame is not null)
            {
                existingGame.Fixes.Add(newFix.Fixes[0]);
            }
            else
            {
                _fixesList.Add(newFix);
            }

            var newFixGameId = newFix.GameId;
            var newFixGuid = newFix.Fixes.Last().Guid;

            return new(ResultEnum.Success, new(newFixGameId, newFixGuid), string.Empty);
        }
        catch (Exception ex)
        {
            return new(ResultEnum.Error, null, ex.Message);
        }
    }

    public void SaveFixesJson(string file)
    {
        var sortedFixesList = _fixesList.OrderBy(x => x.GameName).ToList();

        foreach (var list in sortedFixesList)
        {
            list.Fixes = [.. list.Fixes
                .OrderByDescending(x => !x.IsDisabled)
                .ThenByDescending(x => !x.Name.StartsWith("No Intro"))
                .ThenBy(x => x.Name)
                ];
        }

        foreach (var list in sortedFixesList)
        {
            foreach (var fix in list.Fixes)
            {
                if (fix is FileFixEntity fixEntity &&
                    fixEntity.Url is not null &&
                    (string.IsNullOrWhiteSpace(fixEntity.MD5) || fixEntity.FileSize < 1))
                {
                    if (!fixEntity.Url.StartsWith("http"))
                    {
                        fixEntity.Url = Consts.BucketAddress + fixEntity.Url;
                    }

                    using var header = _httpClient.GetAsync(fixEntity.Url, HttpCompletionOption.ResponseHeadersRead).Result;

                    Guard.IsNotNull(header.Content.Headers.ContentLength);

                    fixEntity.FileSize = header.Content.Headers.ContentLength;

                    if (fixEntity.Url.StartsWith(Consts.BucketAddress))
                    {
                        fixEntity.MD5 = header.Headers.ETag!.Tag.Replace("\"", "");
                    }
                    else if (header.Content.Headers.ContentMD5 is not null)
                    {
                        fixEntity.MD5 = Convert.ToHexString(header.Content.Headers.ContentMD5);
                    }
                    else
                    {
                        using var stream = _httpClient.GetStreamAsync(fixEntity.Url).Result;
                        using var md5 = MD5.Create();

                        byte[] hash = md5.ComputeHash(stream);

                        fixEntity.MD5 = Convert.ToHexString(hash);
                    }
                }
            }
        }

        var jsonString = JsonSerializer.Serialize(sortedFixesList, SourceEntityContext.Default.ListFixesList);

        File.WriteAllText(file, jsonString);
    }
}

