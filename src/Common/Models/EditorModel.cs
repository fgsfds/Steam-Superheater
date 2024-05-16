using Common.Config;
using Common.Entities;
using Common.Entities.Fixes;
using Common.Entities.Fixes.FileFix;
using Common.Entities.Fixes.HostsFix;
using Common.Entities.Fixes.RegistryFix;
using Common.Entities.Fixes.TextFix;
using Common.Enums;
using Common.Helpers;
using Common.Providers.Cached;
using System.Collections.Immutable;
using System.Text;
using System.Text.Json;

namespace Common.Models
{
    public sealed class EditorModel(
        FixesProvider fixesProvider,
        GamesProvider gamesProvider,
        ConfigProvider configProvider,
        FilesUploader filesUploader,
        Logger logger
        )
    {
        private readonly FixesProvider _fixesProvider = fixesProvider;
        private readonly GamesProvider _gamesProvider = gamesProvider;
        private readonly ConfigEntity _config = configProvider.Config;
        private readonly FilesUploader _filesUploader = filesUploader;
        private readonly Logger _logger = logger;

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
                await GetListOfFixesAsync(useCache).ConfigureAwait(false);
                await UpdateListOfAvailableGamesAsync(useCache).ConfigureAwait(false);

                return new(ResultEnum.Success, string.Empty);
            }
            catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
            {
                _logger.Error(ex.Message);
                return new(ResultEnum.NotFound, $"File not found: {ex.Message}");
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                _logger.Error(ex.Message);
                return new(ResultEnum.ConnectionError, "API is not responding");
            }
        }

        /// <summary>
        /// Get list of fixes optionally filtered by a search string
        /// </summary>
        /// <param name="search">Search string</param>
        public ImmutableList<FixesList> GetFilteredGamesList(string? search = null, string? tag = null)
        {
            List<FixesList> result = [.. _fixesList];

            foreach (var entity in result.ToArray())
            {
                foreach (var fix in entity.Fixes)
                {
                    fix.IsHidden = false;
                }
            }

            if (string.IsNullOrEmpty(search) &&
                string.IsNullOrEmpty(tag))
            {
                return [.. result];
            }

            if (!string.IsNullOrEmpty(tag))
            {
                if (!tag.Equals(ConstStrings.All))
                {
                    foreach (var entity in result.ToArray())
                    {
                        foreach (var fix in entity.Fixes)
                        {
                            if (tag.Equals(ConstStrings.WindowsOnly) && fix.SupportedOSes != OSEnum.Windows)
                            {
                                fix.IsHidden = true;
                            }
                            else if (tag.Equals(ConstStrings.LinuxOnly) && fix.SupportedOSes != OSEnum.Linux)
                            {
                                fix.IsHidden = true;
                            }
                            else if (tag.Equals(ConstStrings.AllSuppoted) && fix.SupportedOSes != (OSEnum.Linux | OSEnum.Windows))
                            {
                                fix.IsHidden = true;
                            }
                        }

                        if (!entity.Fixes.Exists(static x => !x.IsHidden))
                        {
                            result.Remove(entity);
                        }
                    }
                }
            }

            if (search is null)
            {
                return [.. result];
            }

            return [.. result.Where(x => x.GameName.Contains(search, StringComparison.CurrentCultureIgnoreCase))];
        }

        /// <summary>
        /// Get list of fixes optionally filtered by a search string
        /// </summary>
        public ImmutableList<GameEntity> GetAvailableGamesList() => [.. _availableGamesList];

        /// <summary>
        /// Get list of fixes optionally filtered by a search string
        /// </summary>
        public ImmutableList<FileFixEntity> GetSharedFixesList() => _fixesProvider.GetSharedFixes();

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

                await UpdateListOfAvailableGamesAsync(true).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Save current fixes list to XML
        /// </summary>
        /// <returns>Result message</returns>
        public async Task<Result> SaveFixesListAsync()
        {
            var saveXmlResult = await _fixesProvider.SaveFixesAsync(_fixesList).ConfigureAwait(false);

            if (!saveXmlResult.IsSuccess)
            {
                return saveXmlResult;
            }

            var createReadmeResult = CreateReadme();

            if (!createReadmeResult.IsSuccess)
            {
                return createReadmeResult;
            }

            return saveXmlResult;
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
                    (fix.Dependencies is null || !fix.Dependencies.Contains(fixEntity.Guid)) &&
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
        public async Task<Result> UploadFixAsync(FixesList fixesList, BaseFixEntity fix, CancellationToken cancellationToken)
        {
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

            if (result == ResultEnum.Success)
            {
                return new(ResultEnum.Success, """
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
            var doesFixExists = await _fixesProvider.CheckIfFixExistsInTheDatabase(fix.Guid).ConfigureAwait(false);

            if (doesFixExists)
            {
                return new(ResultEnum.Error, $"Can't upload fix.{Environment.NewLine}{Environment.NewLine}This fix already exists in the database.");
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
                    return new(ResultEnum.Error, $"Can't upload file larger than 1Gb.{Environment.NewLine}{Environment.NewLine}Please, upload it to file hosting.");
                }
            }

            return new(ResultEnum.Success, string.Empty);
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
        private async Task GetListOfFixesAsync(bool useCache) => _fixesList = [.. await _fixesProvider.GetListAsync(useCache).ConfigureAwait(false)];

        /// <summary>
        /// Create or update list of games that can be added to the fixes list
        /// </summary>
        private async Task UpdateListOfAvailableGamesAsync(bool useCache)
        {
            var installedGames = await _gamesProvider.GetListAsync(useCache).ConfigureAwait(false);

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
        private Result CreateReadme()
        {
            StringBuilder fixesList = new();
            List<string> noIntroGames = [];
            
            var gamesCount = 0;
            var fixesCount = 0;

            foreach (var game in _fixesList)
            {
                var first = true;
                    
                foreach (var fix in game.Fixes)
                {
                    if (fix.Name.StartsWith("No Intro"))
                    {
                        noIntroGames.Add(game.GameName);
                        continue;
                    }

                    if (first)
                    {
                        fixesList.Append($"{Environment.NewLine}{game.GameName}{Environment.NewLine}");
                        gamesCount++;
                        first = false;
                    }

                    fixesList.Append($"- {fix.Name}{Environment.NewLine}");
                    fixesCount++;
                }
            }

            StringBuilder result = new($"**CURRENTLY AVAILABLE {fixesCount} FIXES FOR {gamesCount} GAMES**{Environment.NewLine}{Environment.NewLine}");
            result.Append(fixesList);
            result.Append($"{Environment.NewLine}No Intro Fixes for: {string.Join(", ", noIntroGames)}");

            try
            {
                File.WriteAllText(Path.Combine(_config.LocalRepoPath, "README.md"), result.ToString());
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message);
                return new(ResultEnum.Error, ex.Message);
            }

            return new(ResultEnum.Success, "Readme saved successfully");
        }
    }
}
