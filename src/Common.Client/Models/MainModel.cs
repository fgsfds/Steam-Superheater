using Common.Client.API;
using Common.Client.Config;
using Common.Client.Providers;
using Common.Entities.CombinedEntities;
using Common.Entities.Fixes;
using Common.Entities.Fixes.FileFix;
using Common.Enums;
using Common.Helpers;
using System.Collections.Immutable;

namespace Common.Client.Models
{
    public sealed class MainModel
    {
        private readonly IConfigProvider _config;
        private readonly CombinedEntitiesProvider _combinedEntitiesProvider;
        private readonly ApiInterface _apiInterface;

        private List<FixFirstCombinedEntity> _combinedEntitiesList = [];

        public int UpdateableGamesCount => _combinedEntitiesList.Count(static x => x.HasUpdates);

        public bool HasUpdateableGames => UpdateableGamesCount > 0;


        public MainModel(
            IConfigProvider configProvider,
            CombinedEntitiesProvider combinedEntitiesProvider,
            ApiInterface apiInterface
            )
        {
            _config = configProvider;
            _combinedEntitiesProvider = combinedEntitiesProvider;
            _apiInterface = apiInterface;
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

            var games = result.ResultObject;

            _combinedEntitiesList = games;

            return new(ResultEnum.Success, "Games list updated successfully");
        }

        /// <summary>
        /// Get list of games optionally filtered by a search string
        /// </summary>
        /// <param name="search">Search string</param>
        /// <param name="tag">Selected tag</param>
        public ImmutableList<FixFirstCombinedEntity> GetFilteredGamesList(string? search = null, string? tag = null)
        {
            foreach (var entity in _combinedEntitiesList)
            {
                foreach (var fix in entity.FixesList.Fixes)
                {
                    //remove fixes with hidden tags
                    if (fix.Tags is not null &&
                        fix.Tags.Count != 0 &&
                        fix.Tags.All(_config.HiddenTags.Contains))
                    {
                        fix.IsHidden = true;
                        continue;
                    }

                    //remove fixes for different OSes
                    if (!_config.ShowUnsupportedFixes &&
                        !fix.SupportedOSes.HasFlag(OSEnumHelper.CurrentOSEnum))
                    {
                        fix.IsHidden = true;
                        continue;
                    }

                    if (!entity.IsGameInstalled &&
                        !_config.ShowUninstalledGames)
                    {
                        fix.IsHidden = true;
                        continue;
                    }

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
                    foreach (var entity in _combinedEntitiesList)
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

            if (string.IsNullOrWhiteSpace(search))
            {
                return [.. _combinedEntitiesList.Where(static x => !x.FixesList.IsEmpty)];
            }

            return [.. _combinedEntitiesList.Where(x => x.GameName.Contains(search, StringComparison.CurrentCultureIgnoreCase) && !x.FixesList.IsEmpty)];
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
        public List<BaseFixEntity>? GetDependenciesForAFix(FixFirstCombinedEntity entity, BaseFixEntity fix)
        {
            if (fix.Dependencies is null ||
                fix.Dependencies.Count == 0)
            {
                return null;
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

            return deps is not null && deps.Exists(static x => !x.IsInstalled);
        }

        /// <summary>
        /// Get list of fixes that depend on a selected fix
        /// </summary>
        /// <param name="fixes">List of fix entities</param>
        /// <param name="guid">Guid of a fix</param>
        /// <returns>List of dependent fixes</returns>
        public List<BaseFixEntity> GetDependentFixes(IEnumerable<BaseFixEntity> fixes, Guid guid)
        {
            return [.. fixes.Where(x => x.Dependencies is not null && x.Dependencies.Contains(guid))];
        }

        /// <summary>
        /// Does fix have dependent fixes that are currently installed
        /// </summary>
        /// <param name="fixes">List of fix entities</param>
        /// <param name="guid">Guid of a fix</param>
        /// <returns>true if there are installed dependent fixes</returns>
        public bool DoesFixHaveInstalledDependentFixes(IEnumerable<BaseFixEntity> fixes, Guid guid)
        {
            var deps = GetDependentFixes(fixes, guid);

            return deps is not null && deps.Exists(static x => x.IsInstalled);
        }

        public void HideTag(string tag)
        {
            _config.ChangeTagState(tag, true);
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

                _config.ChangeFixUpvoteState(fix.Guid, needTpUpvote);
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
