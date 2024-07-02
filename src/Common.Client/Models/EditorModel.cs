using Common.Client.FilesTools;
using Common.Client.Providers;
using Common.Entities;
using Common.Entities.Fixes;
using Common.Entities.Fixes.FileFix;
using Common.Entities.Fixes.HostsFix;
using Common.Entities.Fixes.RegistryFix;
using Common.Entities.Fixes.TextFix;
using Common.Enums;
using Common.Helpers;
using System.Collections.Immutable;
using System.Text.Json;

namespace Common.Client.Models
{
    public sealed class EditorModel
    {
        private readonly FixesProvider _fixesProvider;
        private readonly GamesProvider _gamesProvider;
        private readonly FilesUploader _filesUploader;

        private List<FixesList> _fixesList = [];
        private List<GameEntity> _availableGamesList = [];


        public EditorModel(
            FixesProvider fixesProvider,
            GamesProvider gamesProvider,
            FilesUploader filesUploader
            )
        {
            _fixesProvider = fixesProvider;
            _gamesProvider = gamesProvider;
            _filesUploader = filesUploader;
        }


        /// <summary>
        /// Update list of fixes either from cache or by downloading fixes.xml from repo
        /// </summary>
        public async Task<Result> UpdateListsAsync()
        {
            var result = await _fixesProvider.GetFixesListAsync().ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                return new(result.ResultEnum, result.Message);
            }

            _fixesList = result.ResultObject;

            await UpdateListOfAvailableGamesAsync().ConfigureAwait(false);

            return new(ResultEnum.Success, string.Empty);
        }

        /// <summary>
        /// Get list of fixes optionally filtered by a search string
        /// </summary>
        /// <param name="search">Search string</param>
        public ImmutableList<FixesList> GetFilteredGamesList(string? search = null, string? tag = null)
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
                            else if (tag.Equals(Consts.AllSuppoted) && fix.SupportedOSes != (OSEnum.Linux | OSEnum.Windows))
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

            return [.. _fixesList.Where(x => !x.IsEmpty && x.GameName.Contains(search, StringComparison.CurrentCultureIgnoreCase))];
        }

        /// <summary>
        /// List of games that that can be added
        /// </summary>
        public ImmutableList<GameEntity> AvailableGames => [.. _availableGamesList];

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
                Fixes = [new FileFixEntity()]
            };

            _fixesList.Add(newFix);

            _availableGamesList.Remove(game);

            return newFix;
        }

        /// <summary>
        /// Add new fix for a game
        /// </summary>
        /// <param name="game">Game entity</param>
        /// <returns>New fix entity</returns>
        public FileFixEntity AddNewFix(FixesList game)
        {
            FileFixEntity newFix = new();

            game.Fixes.Add(newFix);

            return newFix;
        }

        /// <summary>
        /// Remove fix from a game
        /// </summary>
        /// <param name="fix">Fix entity</param>
        /// <param name="isDisabled">Is fix disabled</param>
        public async Task<Result> ChangeFixDisabledState(BaseFixEntity fix, bool isDisabled)
        {
            var result = await _fixesProvider.ChangeFixDisabledStateAsync(fix.Guid, isDisabled).ConfigureAwait(false);

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
        public ImmutableList<BaseFixEntity>? GetDependenciesForAFix(FixesList? fixesList, BaseFixEntity? fixEntity)
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
        public ImmutableList<BaseFixEntity>? GetListOfAvailableDependencies(FixesList? fixesList, BaseFixEntity? fixEntity)
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
        /// <returns>true if uploaded successfully</returns>
        public async Task<Result> UploadFixAsync(FixesList fixesList, BaseFixEntity fix, CancellationToken cancellationToken)
        {
            var fixFilePath = CreateFixJson(fixesList, fix, false, out _);

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

            var result = await _filesUploader.UploadFilesToFtpAsync(fix.Guid.ToString(), filesToUpload, cancellationToken).ConfigureAwait(false);

            File.Delete(fixFilePath);

            return result;
        }

        public string CreateFixJson(FixesList fixesList, BaseFixEntity fix, bool isTestFix, out FixesList newFixesList)
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

            var fixJson = JsonSerializer.Serialize(newFixesList, FixesListContext.Default.FixesList);

            var fixFilePath = Path.Combine(ClientProperties.WorkingFolder, "fix.json");

            File.WriteAllText(fixFilePath, fixJson);
            return fixFilePath;
        }

        /// <summary>
        /// Check if the file can be uploaded
        /// </summary>
        public async Task<Result> CheckFixBeforeUploadAsync(BaseFixEntity fix)
        {
            var doesFixExists = await _fixesProvider.CheckIfFixExistsInTheDatabase(fix.Guid).ConfigureAwait(false);

            if (doesFixExists)
            {
                return new(
                    ResultEnum.Error, 
                    """
                    Can't upload fix.
                    
                    This fix already exists in the database.
                    """);
            }

            if (string.IsNullOrEmpty(fix.Name) ||
                fix.Version < 1)
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

        public void AddDependencyForFix(BaseFixEntity addTo, BaseFixEntity dependency)
        {
            addTo.Dependencies ??= [];
            addTo.Dependencies.Add(dependency.Guid);
        }

        public void RemoveDependencyForFix(BaseFixEntity addTo, BaseFixEntity dependency)
        {
            addTo.Dependencies.ThrowIfNull();

            addTo.Dependencies.Remove(dependency.Guid);

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
        public void ChangeFixType<T>(List<BaseFixEntity> fixesList, BaseFixEntity fix) where T : BaseFixEntity
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
                ThrowHelper.ArgumentException(nameof(fix));
            }
        }

        /// <summary>
        /// Create or update list of games that can be added to the fixes list
        /// </summary>
        private async Task UpdateListOfAvailableGamesAsync()
        {
            var installedGames = await _gamesProvider.GetGamesListAsync().ConfigureAwait(false);

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

                var newFix = JsonSerializer.Deserialize(json, FixesListContext.Default.FixesList)!;

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
    }
}
