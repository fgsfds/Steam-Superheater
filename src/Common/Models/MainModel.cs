using Common.CombinedEntities;
using Common.Config;
using Common.Entities;
using Common.Entities.Fixes;
using Common.Entities.Fixes.FileFix;
using Common.Enums;
using Common.FixTools;
using Common.Helpers;
using Common.Providers;
using System.Collections.Immutable;

namespace Common.Models
{
    public sealed class MainModel(
        ConfigProvider configProvider,
        CombinedEntitiesProvider combinedEntitiesProvider,
        FixManager fixManager
        )
    {
        private readonly ConfigEntity _config = configProvider?.Config ?? ThrowHelper.ArgumentNullException<ConfigEntity>(nameof(configProvider));
        private readonly CombinedEntitiesProvider _combinedEntitiesProvider = combinedEntitiesProvider ?? ThrowHelper.ArgumentNullException<CombinedEntitiesProvider>(nameof(combinedEntitiesProvider));
        private readonly FixManager _fixManager = fixManager ?? ThrowHelper.ArgumentNullException<FixManager>(nameof(fixManager));
        private readonly List<FixFirstCombinedEntity> _combinedEntitiesList = new();

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
                            fix.Tags.Count != 0 &&
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

            return result.Where(x => x.GameName.Contains(search, StringComparison.CurrentCultureIgnoreCase)).ToImmutableList();
        }

        /// <summary>
        /// Get link to current fix's file
        /// </summary>
        /// <param name="fix">Fix</param>
        public string GetSelectedFixUrl(BaseFixEntity? fix)
        {
            if (fix is not FileFixEntity fileFix)
            {
                return string.Empty;
            }

            if (string.IsNullOrEmpty(fileFix?.Url)) { return string.Empty; }

            return !_config.UseTestRepoBranch
                ? fileFix.Url
                : fileFix.Url.Replace("/master/", "/test/");
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
        public List<BaseFixEntity> GetDependenciesForAFix(FixFirstCombinedEntity entity, BaseFixEntity fix)
        {
            if (fix?.Dependencies is null ||
                fix.Dependencies.Count == 0)
            {
                return new List<BaseFixEntity>();
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
        public bool DoesFixHaveNotInstalledDependencies(FixFirstCombinedEntity entity, BaseFixEntity fix)
        {
            var deps = GetDependenciesForAFix(entity, fix);

            if (deps.Count != 0 &&
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
        public static List<BaseFixEntity> GetDependentFixes(IEnumerable<BaseFixEntity> fixes, Guid guid)
            => fixes.Where(x => x.Dependencies is not null && x.Dependencies.Contains(guid)).ToList();

        /// <summary>
        /// Does fix have dependent fixes that are currently installed
        /// </summary>
        /// <param name="fixes">List of fix entities</param>
        /// <param name="guid">Guid of a fix</param>
        /// <returns>true if there are installed dependent fixes</returns>
        public static bool DoesFixHaveInstalledDependentFixes(IEnumerable<BaseFixEntity> fixes, Guid guid)
        {
            var deps = GetDependentFixes(fixes, guid);

            if (deps.Count != 0 &&
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
        public async Task<Result> InstallFixAsync(GameEntity game, BaseFixEntity fix, string? variant, bool skipMD5Check)
        {
            BaseInstalledFixEntity? installedFix;

            try
            {
                installedFix = await _fixManager.InstallFixAsync(game, fix, variant, skipMD5Check);
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
        public Result UninstallFix(GameEntity game, BaseFixEntity fix)
        {
            if (fix.InstalledFix is null) ThrowHelper.NullReferenceException(nameof(fix.InstalledFix));

            var backupRestoreFailed = false;

            try
            {
                _fixManager.UninstallFix(game, fix.InstalledFix, fix);
            }
            catch (BackwardsCompatibilityException)
            {
                backupRestoreFailed = true;
            }
            catch (IOException ex)
            {
                return new(ResultEnum.FileAccessError, ex.Message);
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
        public async Task<Result> UpdateFixAsync(GameEntity game, BaseFixEntity fix, string? variant, bool skipMD5Check)
        {
            if (fix.InstalledFix is null) ThrowHelper.NullReferenceException(nameof(fix.InstalledFix));

            BaseInstalledFixEntity? installedFix;
            var backupRestoreFailed = false;

            try
            {
                installedFix = await _fixManager.UpdateFixAsync(game, fix, variant, skipMD5Check);
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
            catch (IOException ex)
            {
                return new(ResultEnum.FileAccessError, ex.Message);
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
