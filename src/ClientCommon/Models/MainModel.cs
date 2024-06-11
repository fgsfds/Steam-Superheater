using ClientCommon.API;
using ClientCommon.Config;
using ClientCommon.FixTools;
using ClientCommon.Providers;
using Common;
using Common.Entities;
using Common.Entities.CombinedEntities;
using Common.Entities.Fixes;
using Common.Entities.Fixes.FileFix;
using Common.Enums;
using Common.Helpers;
using System.Collections.Immutable;

namespace ClientCommon.Models
{
    public sealed class MainModel
    {
        private readonly ConfigEntity _config;
        private readonly CombinedEntitiesProvider _combinedEntitiesProvider;
        private readonly FixManager _fixManager;
        private readonly ApiInterface _apiInterface;
        private readonly Logger _logger;

        private ImmutableList<FixFirstCombinedEntity> _combinedEntitiesList = [];

        public int UpdateableGamesCount => _combinedEntitiesList.Count(static x => x.HasUpdates);

        public bool HasUpdateableGames => UpdateableGamesCount > 0;


        public MainModel(
            ConfigProvider configProvider,
            CombinedEntitiesProvider combinedEntitiesProvider,
            FixManager fixManager,
            ApiInterface apiInterface,
            Logger logger
            )
        {
            _config = configProvider.Config;
            _combinedEntitiesProvider = combinedEntitiesProvider;
            _fixManager = fixManager;
            _apiInterface = apiInterface;
            _logger = logger;
        }


        /// <summary>
        /// Update list of games either from cache or by downloading fixes.xml from repo
        /// </summary>
        public async Task<Result> UpdateGamesListAsync()
        {
            var result = await _combinedEntitiesProvider.GetFixFirstEntitiesAsync().ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                return new(result.ResultEnum, result.Message);
            }

            var games = result.ResultObject!;

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

        /// <summary>
        /// Get list of games optionally filtered by a search string
        /// </summary>
        /// <param name="search">Search string</param>
        /// <param name="tag">Selected tag</param>
        public ImmutableList<FixFirstCombinedEntity> GetFilteredGamesList(string? search = null, string? tag = null)
        {
            List<FixFirstCombinedEntity> result = [.. _combinedEntitiesList];

            foreach (var entity in _combinedEntitiesList)
            {
                foreach (var fix in entity.FixesList.Fixes)
                {
                    if (ClientProperties.IsDeveloperMode)
                    {
                        fix.IsHidden = false;
                    }
                    else if (!fix.IsInstalled)
                    {
                        fix.IsHidden = fix.IsDisabled;
                    }
                }
            }

            if (!string.IsNullOrEmpty(tag))
            {
                if (!tag.Equals(Consts.All))
                {
                    foreach (var entity in result)
                    {
                        foreach (var fix in entity.FixesList.Fixes)
                        {
                            if (fix.Tags is null ||
                                !fix.Tags.Exists(x => x.Equals(tag))
                                )
                            {
                                fix.IsHidden = true;
                            }
                        }
                    }
                }
            }

            foreach (var entity in result.ToArray())
            {
                if (!entity.FixesList.Fixes.Exists(static x => !x.IsHidden))
                {
                    result.Remove(entity);
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

            if (string.IsNullOrEmpty(fileFix.Url)) 
            {
                return string.Empty;
            }

            return fileFix.Url;
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

            return [Consts.All, .. list];
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
        public async Task<Result> InstallFixAsync(GameEntity game, BaseFixEntity fix, string? variant, bool skipMD5Check, CancellationToken cancellationToken)
        {
            return await _fixManager.InstallFixAsync(game, fix, variant, skipMD5Check, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Uninstall fix
        /// </summary>
        /// <param name="game">Game entity</param>
        /// <param name="fix">Fix to delete</param>
        /// <returns>Result message</returns>
        public Result UninstallFix(GameEntity game, BaseFixEntity fix)
        {
            return _fixManager.UninstallFix(game, fix);
        }

        /// <summary>
        /// Update fix
        /// </summary>
        /// <param name="game">Game entity</param>
        /// <param name="fix">Fix to update</param>
        /// <param name="variant">Fix variant</param>
        /// <param name="skipMD5Check">Skip check of file's MD5</param>
        /// <returns>Result message</returns>
        public async Task<Result> UpdateFixAsync(GameEntity game, BaseFixEntity fix, string? variant, bool skipMD5Check, CancellationToken cancellationToken)
        {
            return await _fixManager.UpdateFixAsync(game, fix, variant, skipMD5Check, cancellationToken).ConfigureAwait(false);
        }

        public void HideTag(string tag)
        {
            var tags = _config.HiddenTags;
            tags.Add(tag);
            tags = [.. tags.OrderBy(static x => x)];

            _config.HiddenTags = tags;
        }

        public async Task<Result<int?>> ChangeVoteAsync(BaseFixEntity fix, bool needTpUpvote)
        {
            sbyte increment = 0;

            var doesEntryExist = _config.Upvotes.TryGetValue(fix.Guid, out var isUpvote);

            if (doesEntryExist)
            {
                if (isUpvote && needTpUpvote)
                {
                    increment = -1;
                }
                else if (isUpvote && !needTpUpvote)
                {
                    increment = -2;
                }
                else if (!isUpvote && needTpUpvote)
                {
                    increment = 2;
                }
                else if (!isUpvote && !needTpUpvote)
                {
                    increment = 1;
                }
            }
            else
            {
                if (needTpUpvote)
                {
                    increment = 1;
                }
                else
                {
                    increment = -1;
                }
            }

            var response = await _apiInterface.ChangeScoreAsync(fix.Guid, increment).ConfigureAwait(false);

            if (response.IsSuccess)
            {
                fix.Score = response.ResultObject;

                if (doesEntryExist)
                {
                    if (isUpvote && needTpUpvote)
                    {
                        _config.Upvotes.Remove(fix.Guid);
                    }
                    else if (isUpvote && !needTpUpvote)
                    {
                        _config.Upvotes[fix.Guid] = false;
                    }
                    else if (!isUpvote && needTpUpvote)
                    {
                        _config.Upvotes[fix.Guid] = true;
                    }
                    else if (!isUpvote && !needTpUpvote)
                    {
                        _config.Upvotes.Remove(fix.Guid);
                    }
                }
                else
                {
                    if (needTpUpvote)
                    {
                        _config.Upvotes.Add(fix.Guid, true);
                    }
                    else
                    {
                        _config.Upvotes.Add(fix.Guid, false);
                    }
                }
            }

            return response;
        }

        public async Task IncreaseInstalls(BaseFixEntity fix)
        {
            if (ClientProperties.IsDeveloperMode)
            {
                return;
            }

            var result = await _apiInterface.AddNumberOfInstallsAsync(fix.Guid).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                return;
            }

            fix.Installs = result.ResultObject;
        }
    }
}
