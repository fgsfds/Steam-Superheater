using Common.Entities;
using Common.Entities.Fixes;
using Common.Entities.Fixes.FileFix;
using Common.Entities.Fixes.RegistryFix;
using Common.Helpers;
using Common.Providers;
using System.Collections.Immutable;
using System.Xml.Serialization;

namespace Common.Models
{
    public sealed class EditorModel(
        FixesProvider fixesProvider,
        CombinedEntitiesProvider combinedEntitiesProvider,
        GamesProvider gamesProvider
        )
    {
        private readonly FixesProvider _fixesProvider = fixesProvider ?? ThrowHelper.NullReferenceException<FixesProvider>(nameof(fixesProvider));
        private readonly CombinedEntitiesProvider _combinedEntitiesProvider = combinedEntitiesProvider ?? ThrowHelper.NullReferenceException<CombinedEntitiesProvider>(nameof(combinedEntitiesProvider));
        private readonly GamesProvider _gamesProvider = gamesProvider ?? ThrowHelper.NullReferenceException<GamesProvider>(nameof(gamesProvider));

        private readonly List<FixesList> _fixesList = new();
        private readonly List<GameEntity> _availableGamesList = new();

        /// <summary>
        /// Update list of fixes either from cache or by downloading fixes.xml from repo
        /// </summary>
        /// <param name="useCache">Is cache used</param>
        public async Task<Result> UpdateListsAsync(bool useCache)
        {
            try
            {
                await GetListOfFixesAsync(useCache);
                await UpdateListOfAvailableGamesAsync(useCache);

                return new(ResultEnum.Ok, string.Empty);
            }
            catch (Exception ex) when (ex is FileNotFoundException || ex is DirectoryNotFoundException)
            {
                Logger.Error(ex.Message);
                return new(ResultEnum.NotFound, $"File not found: {ex.Message}");
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
            {
                Logger.Error(ex.Message);
                return new(ResultEnum.ConnectionError, "Can't connect to GitHub repository");
            }
        }

        /// <summary>
        /// Get list of fixes optionally filtered by a search string
        /// </summary>
        /// <param name="search">Search string</param>
        public ImmutableList<FixesList> GetFilteredGamesList(string? search = null)
        {
            if (!string.IsNullOrEmpty(search))
            {
                return _fixesList.Where(x => x.GameName.Contains(search, StringComparison.CurrentCultureIgnoreCase)).ToImmutableList();
            }
            else
            {
                return _fixesList.ToImmutableList();
            }
        }

        /// <summary>
        /// Get list of fixes optionally filtered by a search string
        /// </summary>
        /// <param name="search">Search string</param>
        public ImmutableList<GameEntity> GetAvailableGamesList() => _availableGamesList.ToImmutableList();

        /// <summary>
        /// Add new game with empty fix
        /// </summary>
        /// <param name="game">Game entity</param>
        /// <returns>New fixes list</returns>
        public FixesList AddNewGame(GameEntity game)
        {
            FixesList newFix = new(game.Id, game.Name, []);
            newFix.Fixes.Add(new FileFixEntity());

            _fixesList.Add(newFix);

            var newFixesList = _fixesList.OrderBy(x => x.GameName).ToList();
            _fixesList.Clear();
            _fixesList.AddRange(newFixesList);

            _availableGamesList.Remove(game);

            return newFix;
        }

        /// <summary>
        /// Add new fix for a game
        /// </summary>
        /// <param name="game">Game entity</param>
        /// <returns>New fix entity</returns>
        public static FileFixEntity AddNewFix(FixesList game)
        {
            FileFixEntity newFix = new();

            game.Fixes.Add(newFix);

            return newFix;
        }

        /// <summary>
        /// Remove fix from a game
        /// </summary>
        /// <param name="game">Game entity</param>
        /// <param name="fix">Fix entity</param>
        public async void RemoveFix(FixesList game, BaseFixEntity fix)
        {
            game.Fixes.Remove(fix);

            if (game.Fixes.Count == 0)
            {
                _fixesList.Remove(game);

                await UpdateListOfAvailableGamesAsync(true);
            }
        }

        /// <summary>
        /// Save current fixes list to XML
        /// </summary>
        /// <returns>Result message</returns>
        public async Task<Result> SaveFixesListAsync()
        {
            var result = await FixesProvider.SaveFixesAsync(_fixesList);

            CreateReadme();

            return result;
        }

        /// <summary>
        /// Get list of dependencies for a fix
        /// </summary>
        /// <param name="fixesList">Fixes list</param>
        /// <param name="fixEntity">Fix</param>
        /// <returns>List of dependencies</returns>
        public ImmutableList<BaseFixEntity> GetDependenciesForAFix(FixesList? fixesList, BaseFixEntity? fixEntity)
        {
            if (fixEntity?.Dependencies is null ||
                fixesList is null)
            {
                return [];
            }

            var allGameFixes = _fixesList.Where(x => x.GameId == fixesList.GameId).FirstOrDefault();

            if (allGameFixes is null)
            {
                return [];
            }

            var allGameDeps = fixEntity.Dependencies;

            var deps = allGameFixes.Fixes.Where(x => allGameDeps.Contains(x.Guid)).ToList();

            return deps.ToImmutableList();
        }

        /// <summary>
        /// Get list of fixes that can be added as dependencies
        /// </summary>
        /// <param name="fixesList">Fixes list</param>
        /// <param name="fixEntity">Fix</param>
        /// <returns>List of fixes</returns>
        public ImmutableList<BaseFixEntity> GetListOfAvailableDependencies(FixesList? fixesList, BaseFixEntity? fixEntity)
        {
            if (fixesList is null ||
                fixEntity is null)
            {
                return [];
            }

            List<BaseFixEntity> result = new();

            var fixDependencies = GetDependenciesForAFix(fixesList, fixEntity);

            foreach (var fix in fixesList.Fixes)
            {
                if (//don't add itself
                    fix.Guid != fixEntity.Guid &&
                    //don't add fixes that depend on it
                    fix.Dependencies is not null && !fix.Dependencies.Any(x => x == fixEntity.Guid) &&
                    //don't add fixes that are already dependencies
                    !fixDependencies.Where(x => x.Guid == fix.Guid).Any()
                    )
                {
                    result.Add(fix);
                }
            }

            return result.ToImmutableList();
        }

        /// <summary>
        /// Upload fix to ftp
        /// </summary>
        /// <param name="fixesList">Fixes list entity</param>
        /// <param name="fix">New fix</param>
        /// <returns>true if uploaded successfully</returns>
        public static Result UploadFix(FixesList fixesList, BaseFixEntity fix)
        {
            FixesList newFix = new(
                fixesList.GameId,
                fixesList.GameName,
                new List<BaseFixEntity>() { fix }
                );

            var guid = newFix.Fixes.First().Guid;

            string? fileToUpload = null;

            if (newFix.Fixes[0] is FileFixEntity fileFix)
            {
                var url = fileFix.Url;

                if (!string.IsNullOrEmpty(url) &&
                    !url.StartsWith("http"))
                {
                    fileToUpload = fileFix.Url;
                    fileFix.Url = Path.GetFileName(fileToUpload);
                }
            }

            XmlSerializer xmlSerializer = new(typeof(FixesList));

            List<string> filesToUpload = new();

            var fixFilePath = Path.Combine(Directory.GetCurrentDirectory(), "fix.xml");

            using (FileStream fs = new(fixFilePath, FileMode.Create))
            {
                xmlSerializer.Serialize(fs, newFix);
            }

            filesToUpload.Add(fixFilePath);

            if (fileToUpload is not null)
            {
                filesToUpload.Add(fileToUpload);
            }

            var result = FilesUploader.UploadFilesToFtp(guid.ToString(), filesToUpload);

            File.Delete(fixFilePath);

            if (result.ResultEnum is ResultEnum.Ok)
            {
                return new(ResultEnum.Ok, @$"Fix successfully uploaded.
It will be added to the database after developer's review.

Thank you.");
            }
            else
            {
                return new(ResultEnum.Error, result.Message);
            }
        }

        /// <summary>
        /// Check if the file can be uploaded
        /// </summary>
        public async Task<Result> CheckFixBeforeUploadAsync(BaseFixEntity fix)
        {
            if (string.IsNullOrEmpty(fix?.Name) ||
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

                else if (new FileInfo(fileFix.Url).Length > 1e+8)
                {
                    return new(ResultEnum.Error, $"Can't upload file larger than 100Mb.{Environment.NewLine}{Environment.NewLine}Please, upload it to file hosting.");
                }
            }

            var onlineFixes = await _fixesProvider.GetOnlineFixesListAsync();

            foreach (var onlineFix in onlineFixes)
            {
                //if fix already exists in the repo, don't upload it
                if (onlineFix.Fixes.Any(x => x.Guid == fix.Guid))
                {
                    return new(ResultEnum.Error, $"Can't upload fix.{Environment.NewLine}{Environment.NewLine}This fix already exists in the database.");
                }
            }

            return new(ResultEnum.Ok, string.Empty);
        }

        public static void AddDependencyForFix(BaseFixEntity addTo, BaseFixEntity dependency)
        {
            addTo.Dependencies ??= new();
            addTo.Dependencies.Add(dependency.Guid);
        }

        public static void RemoveDependencyForFix(BaseFixEntity addTo, BaseFixEntity dependency)
        {
            addTo.Dependencies ??= new();
            addTo.Dependencies.Remove(dependency.Guid);
        }

        public static void MoveFixUp(List<BaseFixEntity> fixesList, int index) => fixesList.Move(index, index - 1);

        public static void MoveFixDown(List<BaseFixEntity> fixesList, int index) => fixesList.Move(index, index + 1);

        /// <summary>
        /// Change type of the fix
        /// </summary>
        /// <typeparam name="T">New fix type</typeparam>
        /// <param name="fixesList">List of fixes</param>
        /// <param name="fix">Fix to replace</param>
        public static void ChangeFixType<T>(List<BaseFixEntity> fixesList, BaseFixEntity fix) where T : BaseFixEntity
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
            else
            {
                ThrowHelper.ArgumentException(nameof(fix));
            }
        }

        /// <summary>
        /// Get sorted list of fixes
        /// </summary>
        /// <param name="useCache">Use cached list</param>
        private async Task GetListOfFixesAsync(bool useCache)
        {
            _fixesList.Clear();

            var fixes = await _combinedEntitiesProvider.GetFixesListAsync(useCache);

            fixes = fixes.OrderBy(x => x.GameName).ToList();

            foreach (var fix in fixes)
            {
                _fixesList.Add(fix);
            }
        }

        /// <summary>
        /// Create or update list of games that can be added to the fixes list
        /// </summary>
        private async Task UpdateListOfAvailableGamesAsync(bool useCache)
        {
            _availableGamesList.Clear();

            var installedGames = useCache
                ? await _gamesProvider.GetCachedListAsync()
                : await _gamesProvider.GetNewListAsync();

            foreach (var game in installedGames)
            {
                if (!_fixesList.Any(x => x.GameId == game.Id))
                {
                    _availableGamesList.Add(game);
                }
            }
        }

        /// <summary>
        /// Create readme file containing list of fixes
        /// </summary>
        private void CreateReadme()
        {
            var result = "**CURRENTLY AVAILABLE FIXES**" + Environment.NewLine + Environment.NewLine;

            foreach (var fix in _fixesList)
            {
                result += fix.GameName + Environment.NewLine;

                foreach (var f in fix.Fixes)
                {
                    result += "- " + f.Name + Environment.NewLine;
                }

                result += Environment.NewLine;
            }

            File.WriteAllText(Path.Combine(CommonProperties.LocalRepoPath, "README.md"), result);
        }
    }
}
