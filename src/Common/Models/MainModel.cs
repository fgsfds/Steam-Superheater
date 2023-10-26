using Common.CombinedEntities;
using Common.Config;
using Common.Entities;
using Common.Enums;
using Common.FixTools;
using Common.Helpers;
using Common.Providers;
using System.Collections.Immutable;

namespace Common.Models
{
    public sealed class MainModel
    {
        public MainModel(
            ConfigProvider configProvider,
            CombinedEntitiesProvider combinedEntitiesProvider,
            FixInstaller fixInstaller,
            FixUpdater fixUpdater
            )
        {
            _combinedEntitiesList = new();
            _config = configProvider?.Config ?? throw new NullReferenceException(nameof(configProvider));
            _combinedEntitiesProvider = combinedEntitiesProvider ?? throw new NullReferenceException(nameof(combinedEntitiesProvider));
            _fixInstaller = fixInstaller ?? throw new NullReferenceException(nameof(fixInstaller));
            _fixUpdater = fixUpdater ?? throw new NullReferenceException(nameof(fixUpdater));
        }

        private readonly ConfigEntity _config;
        private readonly CombinedEntitiesProvider _combinedEntitiesProvider;
        private readonly FixInstaller _fixInstaller;
        private readonly FixUpdater _fixUpdater;

        private readonly List<FixFirstCombinedEntity> _combinedEntitiesList;

        public int UpdateableGamesCount => _combinedEntitiesList.Count(x => x.HasUpdates);

        public bool HasUpdateableGames => UpdateableGamesCount > 0;

        /// <summary>
        /// Update list of games either from cache or by downloading fixes.xml from repo
        /// </summary>
        /// <param name="useCache">Is cache used</param>
        public async Task<Result> UpdateGamesListAsync(bool useCache)
        {
            _combinedEntitiesList.Clear();

            try
            {
                var games = await _combinedEntitiesProvider.GetFixFirstEntitiesAsync(useCache);

                foreach (var game in games.ToList())
                {
                    //remove uninstalled games
                    if (!_config.ShowUninstalledGames &&
                        !game.IsGameInstalled)
                    {
                        games.Remove(game);
                    }

                    foreach (var fix in game.FixesList.Fixes.ToList())
                    {
                        //remove fixes with hidden tags
                        if (fix.Tags is not null &&
                            fix.Tags.All(x => _config.HiddenTags.Contains(x)))
                        {
                            game.FixesList.Fixes.Remove(fix);
                            continue;
                        }

                        //remove fixes for different OSes
                        if (!_config.ShowUnsupportedFixes &&
                            !fix.SupportedOSes.HasFlag(OSEnumHelper.GetCurrentOSEnum()))
                        {
                            game.FixesList.Fixes.Remove(fix);
                            continue;
                        }
                    }

                    //remove games with no shown fixes
                    if (!game.FixesList.Fixes.Any(x => !x.IsHidden))
                    {
                        games.Remove(game);
                    }
                }

                _combinedEntitiesList.AddRange(games);

                return new(ResultEnum.Ok, "Games list updated successfully");
            }
            catch (Exception ex) when (ex is FileNotFoundException || ex is DirectoryNotFoundException)
            {
                return new(ResultEnum.NotFound, "File not found: " + ex.Message);
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
            {
                return new(ResultEnum.ConnectionError, "Can't connect to GitHub repository");
            }
        }

        /// <summary>
        /// Get list of games optionally filtered by a search string
        /// </summary>
        /// <param name="search">Search string</param>
        public ImmutableList<FixFirstCombinedEntity> GetFilteredGamesList(string? search = null, string? tag = null)
        {
            List<FixFirstCombinedEntity> result = _combinedEntitiesList.ToList();

            foreach (var entity in result.ToList())
            {
                foreach (var fix in entity.FixesList.Fixes)
                {
                    fix.IsHidden = false;
                }
            }

            if (string.IsNullOrEmpty(search) &&
                string.IsNullOrEmpty(tag))
            {
                return result.ToImmutableList();
            }

            if (!string.IsNullOrEmpty(tag))
            {
                if (!tag.Equals("All tags"))
                {
                    foreach (var entity in result.ToList())
                    {
                        foreach (var fix in entity.FixesList.Fixes)
                        {
                            if (fix.Tags is not null &&
                                !fix.Tags.Any(x => x.Equals(tag)))
                            {
                                fix.IsHidden = true;
                            }
                        }

                        if (!entity.FixesList.Fixes.Any(x => !x.IsHidden))
                        {
                            result.Remove(entity);
                        }
                    }
                }
            }

            if (search is null)
            {
                return result.ToImmutableList();
            }

            return result.Where(x => x.GameName.ToLower().Contains(search.ToLower())).ToImmutableList();
        }

        /// <summary>
        /// Get link to current fix's file
        /// </summary>
        /// <param name="fix">Fix</param>
        public string GetSelectedFixUrl(FixEntity? fix)
        {
            if (string.IsNullOrEmpty(fix?.Url))
            {
                return string.Empty;
            }

            return !_config.UseTestRepoBranch
                ? fix.Url
                : fix.Url.Replace("/master/", "/test/");
        }

        /// <summary>
        /// Get list of all tags
        /// </summary>
        public HashSet<string> GetListOfTags()
        {
            List<string> result = new() { "All tags" };
            HashSet<string> list = new();

            var games = _combinedEntitiesList;

            foreach (var entity in games)
            {
                foreach (var game in entity.FixesList.Fixes)
                {
                    if (game.Tags is null)
                    {
                        continue;
                    }

                    foreach (var tag in game.Tags)
                    {
                        if (!_config.HiddenTags.Contains(tag))
                        {
                            list.Add(tag);
                        }
                    }
                }

                list = list.OrderBy(x => x).ToHashSet();
            }

            return result.Concat(list).ToHashSet();
        }

        /// <summary>
        /// Get list of dependencies for a fix
        /// </summary>
        /// <param name="entity">Combined entity</param>
        /// <param name="fix">Fix entity</param>
        /// <returns>List of dependencies</returns>
        public List<FixEntity> GetDependenciesForAFix(FixFirstCombinedEntity entity, FixEntity fix)
        {
            if (fix?.Dependencies is null ||
                fix.Dependencies.Count == 0)
            {
                return new List<FixEntity>();
            }

            var allGameFixes = _combinedEntitiesList.Where(x => x.GameName == entity.GameName).First().FixesList;

            var allGameDeps = fix.Dependencies;

            var deps = allGameFixes.Fixes.Where(x => allGameDeps.Contains(x.Guid)).ToList();

            return deps;
        }

        /// <summary>
        /// Does fix have dependencies that are currently not installed
        /// </summary>
        /// <param name="entity">Combined entity</param>
        /// <param name="fix">Fix entity</param>
        /// <returns>true if there are installed dependencies</returns>
        public bool DoesFixHaveNotInstalledDependencies(FixFirstCombinedEntity entity, FixEntity fix)
        {
            var deps = GetDependenciesForAFix(entity, fix);

            if (deps.Any() &&
                deps.Where(x => !x.IsInstalled).Any())
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Get list of fixes that depend on a selected fix
        /// </summary>
        /// <param name="fixes">List of fix entities</param>
        /// <param name="guid">Guid of a fix</param>
        /// <returns>List of dependent fixes</returns>
        public static List<FixEntity> GetDependentFixes(IEnumerable<FixEntity> fixes, Guid guid)
            => fixes.Where(x => x.Dependencies is not null && x.Dependencies.Contains(guid)).ToList();

        /// <summary>
        /// Does fix have dependent fixes that are currently installed
        /// </summary>
        /// <param name="fixes">List of fix entities</param>
        /// <param name="guid">Guid of a fix</param>
        /// <returns>true if there are installed dependent fixes</returns>
        public static bool DoesFixHaveInstalledDependentFixes(IEnumerable<FixEntity> fixes, Guid guid)
        {
            var deps = GetDependentFixes(fixes, guid);

            if (deps.Any() &&
                deps.Where(x => x.IsInstalled).Any())
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Install fix
        /// </summary>
        /// <param name="game">Game entity</param>
        /// <param name="fix">Fix to install</param>
        /// <returns>Result message</returns>
        public async Task<Result> InstallFixAsync(GameEntity game, FixEntity fix, string? variant, bool skipMD5Check)
        {
            InstalledFixEntity? installedFix;

            try
            {
                installedFix = await _fixInstaller.InstallFix(game, fix, variant, skipMD5Check);
            }
            catch (HashCheckFailedException)
            {
                return new(ResultEnum.MD5Error, "MD5 of the file doesn't match the database");
            }
            catch (Exception ex)
            {
                return new(ResultEnum.Error, "Error while installing fix: " + Environment.NewLine + Environment.NewLine + ex.Message);
            }

            fix.InstalledFix = installedFix;

            var result = InstalledFixesProvider.SaveInstalledFixes(_combinedEntitiesList);

            if (result.IsSuccess)
            {
                return new(ResultEnum.Ok, "Fix installed successfully!");
            }
            else
            {
                return new(ResultEnum.Error, result.Message);
            }
        }

        /// <summary>
        /// Uninstall fix
        /// </summary>
        /// <param name="game">Game entity</param>
        /// <param name="fix">Fix to delete</param>
        /// <returns>Result message</returns>
        public Result UninstallFix(GameEntity game, FixEntity fix)
        {
            if (fix.InstalledFix is null) throw new NullReferenceException(nameof(fix.InstalledFix));

            var backupRestoreFailed = false;

            try
            {
                FixUninstaller.UninstallFix(game, fix.InstalledFix, fix);
            }
            catch (BackwardsCompatibilityException)
            {
                backupRestoreFailed = true;
            }

            fix.InstalledFix = null;

            var result = InstalledFixesProvider.SaveInstalledFixes(_combinedEntitiesList);

            if (backupRestoreFailed)
            {
                return new(ResultEnum.Error, "Error while restoring backed up files. Verify integrity of game files on Steam.");
            }
            else if (result.IsSuccess)
            {
                return new(ResultEnum.Ok, "Fix uninstalled successfully!");
            }
            else
            {
                return new(ResultEnum.Error, result.Message);
            }
        }

        /// <summary>
        /// Update fix
        /// </summary>
        /// <param name="game">Game entity</param>
        /// <param name="fix">Fix to update</param>
        /// <returns>Result message</returns>
        public async Task<Result> UpdateFixAsync(GameEntity game, FixEntity fix, string? variant, bool skipMD5Check)
        {
            if (fix.InstalledFix is null) throw new NullReferenceException(nameof(fix.InstalledFix));

            InstalledFixEntity? installedFix;
            var backupRestoreFailed = false;

            try
            {
                installedFix = await _fixUpdater.UpdateFixAsync(game, fix, variant, skipMD5Check);
            }
            catch (BackwardsCompatibilityException)
            {
                backupRestoreFailed = true;
                installedFix = null;
            }
            catch (HashCheckFailedException)
            {
                return new(ResultEnum.MD5Error, "MD5 of the file doesn't match the database");
            }
            catch (Exception ex)
            {
                return new(ResultEnum.Error, "Error while installing fix: " + Environment.NewLine + Environment.NewLine + ex.Message);
            }

            fix.InstalledFix = installedFix;

            var result = InstalledFixesProvider.SaveInstalledFixes(_combinedEntitiesList);

            if (backupRestoreFailed)
            {
                return new(ResultEnum.Error, "Error while restoring backed up files. Verify integrity of game files on Steam.");
            }
            else if (result.IsSuccess)
            {
                return new(ResultEnum.Ok, "Fix updater successfully!");
            }
            else
            {
                return new(ResultEnum.Error, result.Message);
            }
        }
    }
}
