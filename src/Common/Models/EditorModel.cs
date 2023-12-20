using Common.Entities;
using Common.Entities.Fixes;
using Common.Entities.Fixes.FileFix;
using Common.Entities.Fixes.HostsFix;
using Common.Entities.Fixes.RegistryFix;
using Common.Entities.Fixes.TextFix;
using Common.Helpers;
using Common.Providers;
using System.Collections.Immutable;
using Common.Config;
using System.Text;
using System.Text.Json;
using System.Xml;

namespace Common.Models
{
    public sealed class EditorModel(
        FixesProvider _fixesProvider,
        GamesProvider _gamesProvider,
        ConfigProvider configProvider
        )
    {
        private readonly ConfigEntity _config = configProvider.Config;

        private List<FixesList> _fixesList = [];
        private List<GameEntity> _availableGamesList = [];

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
            catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
            {
                Logger.Error(ex.Message);
                return new(ResultEnum.NotFound, $"File not found: {ex.Message}");
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
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
                return [.. _fixesList.Where(x => x.GameName.Contains(search, StringComparison.CurrentCultureIgnoreCase))];
            }

            return [.. _fixesList];
        }

        /// <summary>
        /// Get list of fixes optionally filtered by a search string
        /// </summary>
        public ImmutableList<GameEntity> GetAvailableGamesList() => [.. _availableGamesList];

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

            _fixesList = [.. _fixesList.OrderBy(static x => x.GameName)];

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
            var result = await _fixesProvider.SaveFixesAsync(_fixesList);

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

            var allGameFixes = _fixesList.FirstOrDefault(x => x.GameId == fixesList.GameId);

            if (allGameFixes is null)
            {
                return [];
            }

            var allGameDeps = fixEntity.Dependencies;

            List<BaseFixEntity> deps = [.. allGameFixes.Fixes.Where(x => allGameDeps.Contains(x.Guid))];

            return [.. deps];
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

            List<BaseFixEntity> result = [];

            var fixDependencies = GetDependenciesForAFix(fixesList, fixEntity);

            foreach (var fix in fixesList.Fixes)
            {
                if (//don't add itself
                    fix.Guid != fixEntity.Guid &&
                    //don't add fixes that depend on it
                    fix.Dependencies is not null && !fix.Dependencies.Contains(fixEntity.Guid) &&
                    //don't add fixes that are already dependencies
                    !fixDependencies.Exists(x => x.Guid == fix.Guid)
                    )
                {
                    result.Add(fix);
                }
            }

            return [.. result];
        }

        /// <summary>
        /// Upload fix to ftp
        /// </summary>
        /// <param name="fixesList">Fixes list entity</param>
        /// <param name="fix">New fix</param>
        /// <returns>true if uploaded successfully</returns>
        public static Result UploadFix(FixesList fixesList, BaseFixEntity fix)
        {
            string? fileToUpload = null;

            if (fix is FileFixEntity fileFix)
            {
                var url = fileFix.Url;

                if (!string.IsNullOrEmpty(url) &&
                    !url.StartsWith("http"))
                {
                    fileToUpload = fileFix.Url;
                    fileFix.Url = Path.GetFileName(fileToUpload);
                }
            }

            var newFixesList = new FixesList()
            {
                GameId = fixesList.GameId,
                GameName = fixesList.GameName,
                Fixes = [fix]
            };

            var fixJson = JsonSerializer.Serialize(newFixesList, FixesListContext.Default.FixesList);

            var fixFilePath = Path.Combine(Directory.GetCurrentDirectory(), "fix.xml");

            File.WriteAllText(fixFilePath, fixJson);

            List<string> filesToUpload = [fixFilePath];

            if (fileToUpload is not null)
            {
                filesToUpload.Add(fileToUpload);
            }

            var result = FilesUploader.UploadFilesToFtp(fix.Guid.ToString(), filesToUpload);

            File.Delete(fixFilePath);

            if (result == ResultEnum.Ok)
            {
                return new(ResultEnum.Ok, """
                    Fix successfully uploaded.
                    It will be added to the database after developer's review.

                    Thank you.
                    """);
            }

            return new(ResultEnum.Error, result.Message);
        }

        /// <summary>
        /// Check if the file can be uploaded
        /// </summary>
        public async Task<Result> CheckFixBeforeUploadAsync(BaseFixEntity fix)
        {
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

                if (new FileInfo(fileFix.Url).Length > 1e+8)
                {
                    return new(ResultEnum.Error, $"Can't upload file larger than 100Mb.{Environment.NewLine}{Environment.NewLine}Please, upload it to file hosting.");
                }
            }

            var onlineFixes = await _fixesProvider.GetOnlineFixesListAsync();

            foreach (var onlineFix in onlineFixes)
            {
                //if fix already exists in the repo, don't upload it
                if (onlineFix.Fixes.Exists(x => x.Guid == fix.Guid))
                {
                    return new(ResultEnum.Error, $"Can't upload fix.{Environment.NewLine}{Environment.NewLine}This fix already exists in the database.");
                }
            }

            return new(ResultEnum.Ok, string.Empty);
        }

        public static void AddDependencyForFix(BaseFixEntity addTo, BaseFixEntity dependency)
        {
            addTo.Dependencies ??= [];
            addTo.Dependencies.Add(dependency.Guid);
        }

        public static void RemoveDependencyForFix(BaseFixEntity addTo, BaseFixEntity dependency)
        {
            addTo.Dependencies?.Remove(dependency.Guid);
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
        /// Get sorted list of fixes
        /// </summary>
        /// <param name="useCache">Use cached list</param>
        private async Task GetListOfFixesAsync(bool useCache) => _fixesList = [.. await _fixesProvider.GetListAsync(useCache)];

        /// <summary>
        /// Create or update list of games that can be added to the fixes list
        /// </summary>
        private async Task UpdateListOfAvailableGamesAsync(bool useCache)
        {
            var installedGames = await _gamesProvider.GetListAsync(useCache);

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
        /// Create readme file containing list of fixes
        /// </summary>
        private void CreateReadme()
        {
            var result = "**CURRENTLY AVAILABLE FIXES**" + Environment.NewLine + Environment.NewLine;

            StringBuilder sb = new("No Intro Fixes for: ");
            var first = true;

            foreach (var fix in _fixesList)
            {
                var woNoIntro = fix.Fixes.Where(x => !x.Name.StartsWith("No Intro Fix"));

                if (woNoIntro.Any())
                {
                    result += fix.GameName + Environment.NewLine;

                    foreach (var f in woNoIntro)
                    {
                        result += "- " + f.Name + Environment.NewLine;
                    }

                    result += Environment.NewLine;
                }

                if (fix.Fixes.Exists(x => x.Name.StartsWith("No Intro Fix")))
                {
                    if (first)
                    {
                        sb.Append(fix.GameName);
                        first = false;
                    }

                    sb.Append(", ").Append(fix.GameName);
                }
            }

            result += sb;

            File.WriteAllText(Path.Combine(_config.LocalRepoPath, "README.md"), result);
        }
    }
}
