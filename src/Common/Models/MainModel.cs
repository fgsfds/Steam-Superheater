﻿using Common.Config;
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

        private ImmutableList<FixFirstCombinedEntity> _combinedEntitiesList = [];

        public int UpdateableGamesCount => _combinedEntitiesList.Count(static x => x.HasUpdates);

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
                        !game.IsGameInstalled)
                    {
                        games.Remove(game);
                    }

                    foreach (var fix in game.FixesList.Fixes.ToArray())
                    {
                        //remove fixes with hidden tags
                        if (fix.Tags is not null &&
                            fix.Tags.Count != 0 &&
                            fix.Tags.All(_config.HiddenTags.Contains))
                        {
                            game.FixesList.Fixes.Remove(fix);
                            continue;
                        }

                        //remove fixes for different OSes
                        if (!_config.ShowUnsupportedFixes &&
                            !fix.SupportedOSes.HasFlag(OSEnumHelper.GetCurrentOSEnum()))
                        {
                            game.FixesList.Fixes.Remove(fix);
                        }
                    }

                    //remove games with no shown fixes
                    if (!game.FixesList.Fixes.Exists(static x => !x.IsHidden))
                    {
                        games.Remove(game);
                    }
                }

                _combinedEntitiesList = [.. games];

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
        public ImmutableList<FixFirstCombinedEntity> GetFilteredGamesList(string? search = null, string? tag = null)
        {
            List<FixFirstCombinedEntity> result = [.. _combinedEntitiesList];

            foreach (var entity in result.ToArray())
            {
                foreach (var fix in entity.FixesList.Fixes)
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
                        foreach (var fix in entity.FixesList.Fixes)
                        {
                            if (fix.Tags is null ||
                                !fix.Tags.Exists(x => x.Equals(tag)))
                            {
                                fix.IsHidden = true;
                            }
                        }

                        if (!entity.FixesList.Fixes.Exists(static x => !x.IsHidden))
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
                foreach (var fix in entity.FixesList.Fixes)
                {
                    if (fix.Tags is null)
                    {
                        continue;
                    }

                    foreach (var tag in fix.Tags)
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

            var allGameFixes = _combinedEntitiesList.First(x => x.GameName == entity.GameName).FixesList;

            var allGameDeps = fix.Dependencies;

            List<BaseFixEntity> deps = [.. allGameFixes.Fixes.Where(x => allGameDeps.Contains(x.Guid))];

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
