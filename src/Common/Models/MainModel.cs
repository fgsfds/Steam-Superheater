using Common.Config;
using Common.Entities;
using Common.Entities.CombinedEntities;
using Common.Entities.Fixes;
using Common.Entities.Fixes.FileFix;
using Common.Enums;
using Common.FixTools;
using Common.Helpers;
using System.Collections.Immutable;
using Common.Providers;

namespace Common.Models
{
    public sealed class MainModel(
        ConfigProvider configProvider,
        CombinedEntitiesProvider combinedEntitiesProvider,
        FixManager fixManager
        )
    {
        private readonly ConfigEntity _config = configProvider.Config;
        private readonly CombinedEntitiesProvider _combinedEntitiesProvider = combinedEntitiesProvider;
        private readonly FixManager _fixManager = fixManager;

        private Dictionary<string, FixFirstCombinedEntity> _combinedEntitiesList = [];

        public int UpdateableGamesCount => _combinedEntitiesList.Count(static x => x.Value.HasUpdates);

        public bool HasUpdateableGames => UpdateableGamesCount > 0;

        /// <summary>
        /// Update list of games either from cache or by downloading fixes.xml from repo
        /// </summary>
        /// <param name="useCache">Is cache used</param>
        public async Task<Result> UpdateGamesListAsync(bool useCache)
        {
            try
            {
                var games = await _combinedEntitiesProvider.GetFixFirstEntitiesAsync(useCache);

                foreach (var game in games.ToArray())
                {
                    //remove uninstalled games
                    if (!_config.ShowUninstalledGames &&
                        !game.Value.IsGameInstalled)
                    {
                        games.Remove(game.Key);
                    }

                    foreach (var fix in game.Value.FixesList.Fixes.ToArray())
                    {
                        //remove fixes with hidden tags
                        if (fix.Value.Tags is not null &&
                            fix.Value.Tags.Count != 0 &&
                            fix.Value.Tags.All(_config.HiddenTags.Contains))
                        {
                            game.Value.FixesList.Fixes.Remove(fix.Key);
                            continue;
                        }

                        //remove fixes for different OSes
                        if (!_config.ShowUnsupportedFixes &&
                            !fix.Value.SupportedOSes.HasFlag(OSEnumHelper.GetCurrentOSEnum()))
                        {
                            game.Value.FixesList.Fixes.Remove(fix.Key);
                        }
                    }

                    //remove games with no shown fixes
                    if (!game.Value.FixesList.Fixes.Values.Any(static x => !x.IsHidden))
                    {
                        games.Remove(game.Key);
                    }
                }

                _combinedEntitiesList = games;

                return new(ResultEnum.Success, "Games list updated successfully");
            }
            catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
            {
                Logger.Error(ex.Message);
                return new(ResultEnum.NotFound, "File not found: " + ex.Message);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                Logger.Error(ex.Message);
                return new(ResultEnum.ConnectionError, "Can't connect to GitHub repository");
            }
        }

        /// <summary>
        /// Get list of games optionally filtered by a search string
        /// </summary>
        /// <param name="search">Search string</param>
        /// <param name="tag">Selected tag</param>
        public IDictionary<string, FixFirstCombinedEntity> GetFilteredGamesList(string? search = null, string? tag = null)
        {
            if (string.IsNullOrEmpty(search) &&
                (tag?.Equals(ConstStrings.All) ?? true))
            {
                foreach (var entity in _combinedEntitiesList)
                {
                    foreach (var fix in entity.Value.FixesList.Fixes)
                    {
                        fix.Value.IsHidden = false;
                    }
                }

                return _combinedEntitiesList;
            }

            IDictionary<string, FixFirstCombinedEntity> result = new Dictionary<string, FixFirstCombinedEntity>();

            foreach (var entity in _combinedEntitiesList)
            {
                if (!string.IsNullOrEmpty(search) &&
                    !entity.Key.Contains(search, StringComparison.CurrentCultureIgnoreCase))
                {
                    continue;
                }

                var hasFixes = false;

                foreach (var fix in entity.Value.FixesList.Fixes)
                {
                    if (tag is null ||
                        tag.Equals(ConstStrings.All))
                    {
                        hasFixes = true;
                        fix.Value.IsHidden = false;
                        continue;
                    }

                    if (fix.Value.Tags is not null &&
                        !fix.Value.Tags.Exists(x => x.Equals(tag)))
                    {
                        fix.Value.IsHidden = true;
                    }
                    else if (fix.Value.Tags is null && tag is not null)
                    {
                        fix.Value.IsHidden = true;
                    }
                    else
                    {
                        hasFixes = true;
                        fix.Value.IsHidden = false;
                    }
                }

                if (hasFixes)
                {
                    result.Add(entity);
                }
            }

            return result;
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

            if (string.IsNullOrEmpty(fileFix.Url)) { return string.Empty; }

            return !_config.UseTestRepoBranch
                ? fileFix.Url
                : fileFix.Url.Replace("/master/", "/test/");
        }

        /// <summary>
        /// Get list of all tags
        /// </summary>
        public HashSet<string> GetListOfTags()
        {
            HashSet<string> list = [];

            foreach (var entity in _combinedEntitiesList)
            {
                foreach (var fix in entity.Value.FixesList.Fixes)
                {
                    if (fix.Value.Tags is null)
                    {
                        continue;
                    }

                    foreach (var tag in fix.Value.Tags)
                    {
                        if (!_config.HiddenTags.Contains(tag))
                        {
                            list.Add(tag);
                        }
                    }
                }

                list = [.. list.OrderBy(static x => x)];
            }

            return [ConstStrings.All, .. list];
        }

        /// <summary>
        /// Get list of dependencies for a fix
        /// </summary>
        /// <param name="entity">Combined entity</param>
        /// <param name="fix">Fix entity</param>
        /// <returns>List of dependencies</returns>
        public List<BaseFixEntity> GetDependenciesForAFix(FixFirstCombinedEntity entity, BaseFixEntity fix)
        {
            if (fix.Dependencies is null ||
                fix.Dependencies.Count == 0)
            {
                return [];
            }

            var allGameFixes = _combinedEntitiesList[entity.GameName].FixesList;

            var allGameDeps = fix.Dependencies;

            var deps = allGameFixes.Fixes.Where(x => allGameDeps.Contains(x.Key)).Select(static x => x.Value);

            return deps.ToList();
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

            return deps.Count != 0 &&
                   deps.Exists(static x => !x.IsInstalled);
        }

        /// <summary>
        /// Get list of fixes that depend on a selected fix
        /// </summary>
        /// <param name="fixes">List of fix entities</param>
        /// <param name="guid">Guid of a fix</param>
        /// <returns>List of dependent fixes</returns>
        public List<BaseFixEntity> GetDependentFixes(IEnumerable<BaseFixEntity> fixes, Guid guid)
            => [.. fixes.Where(x => x.Dependencies is not null && x.Dependencies.Contains(guid))];

        /// <summary>
        /// Does fix have dependent fixes that are currently installed
        /// </summary>
        /// <param name="fixes">List of fix entities</param>
        /// <param name="guid">Guid of a fix</param>
        /// <returns>true if there are installed dependent fixes</returns>
        public bool DoesFixHaveInstalledDependentFixes(IEnumerable<BaseFixEntity> fixes, Guid guid)
        {
            var deps = GetDependentFixes(fixes, guid);

            return deps.Count != 0 &&
                   deps.Exists(static x => x.IsInstalled);
        }

        /// <summary>
        /// Install fix
        /// </summary>
        /// <param name="game">Game entity</param>
        /// <param name="fix">Fix to install</param>
        /// <param name="variant">Fix variant</param>
        /// <param name="skipMD5Check">Skip check of file's MD5</param>
        /// <returns>Result message</returns>
        public async Task<Result> InstallFixAsync(GameEntity game, BaseFixEntity fix, string? variant, bool skipMD5Check) =>
            await _fixManager.InstallFixAsync(game, fix, variant, skipMD5Check);

        /// <summary>
        /// Uninstall fix
        /// </summary>
        /// <param name="game">Game entity</param>
        /// <param name="fix">Fix to delete</param>
        /// <returns>Result message</returns>
        public Result UninstallFix(GameEntity game, BaseFixEntity fix) =>
            _fixManager.UninstallFix(game, fix);

        /// <summary>
        /// Update fix
        /// </summary>
        /// <param name="game">Game entity</param>
        /// <param name="fix">Fix to update</param>
        /// <param name="variant">Fix variant</param>
        /// <param name="skipMD5Check">Skip check of file's MD5</param>
        /// <returns>Result message</returns>
        public async Task<Result> UpdateFixAsync(GameEntity game, BaseFixEntity fix, string? variant, bool skipMD5Check) =>
            await _fixManager.UpdateFixAsync(game, fix, variant, skipMD5Check);

        public void HideTag(string tag)
        {
            var tags = _config.HiddenTags;
            tags.Add(tag);
            tags = [.. tags.OrderBy(static x => x)];

            _config.HiddenTags = tags;
        }
    }
}
