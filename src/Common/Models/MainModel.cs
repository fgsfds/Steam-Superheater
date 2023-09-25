using Common.CombinedEntities;
using Common.Config;
using Common.Entities;
using Common.FixTools;
using Common.Providers;

namespace Common.Models
{
    public class MainModel
    {
        private readonly List<FixFirstCombinedEntity> _combinedEntitiesList;
        private readonly ConfigEntity _config;

        public int UpdateableGamesCount => _combinedEntitiesList.Count(x => x.HasUpdates);

        public bool HasUpdateableGames => UpdateableGamesCount > 0;

        public MainModel(ConfigProvider configProvider)
        {
            _combinedEntitiesList = new();
            _config = configProvider?.Config ?? throw new NullReferenceException(nameof(configProvider));
        }

        /// <summary>
        /// Update list of games either from cache or by downloading fixes.xml from repo
        /// </summary>
        /// <param name="useCache">Is cache used</param>
        public async Task UpdateGamesListAsync(bool useCache)
        {
            _combinedEntitiesList.Clear();

            var games = await CombinedEntitiesProvider.GetFixFirstEntitiesAsync(useCache);

            _combinedEntitiesList.AddRange(games);
        }

        /// <summary>
        /// Get list of games optionally filtered by a search string
        /// </summary>
        /// <param name="search">Search string</param>
        public List<FixFirstCombinedEntity> GetFilteredGamesList(string? search = null)
        {
            List<FixFirstCombinedEntity> result = _combinedEntitiesList;

            //result = _combinedEntitiesList.Where(x => x.FixesList.Fixes?.Count > 0).ToList();

            if (!_config.ShowUninstalledGames)
            {
                result = result.Where(x => x.IsGameInstalled).ToList();
            }

            if (!string.IsNullOrEmpty(search))
            {
                result = result.Where(x => x.GameName.ToLower().Contains(search.ToLower())).ToList();
            }

            return result;
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
        /// Does fix have dependencies that are currently installed
        /// </summary>
        /// <param name="entity">Combined entity</param>
        /// <param name="fix">Fix entity</param>
        /// <returns>true if there are installed dependencies</returns>
        public bool DoesFixHaveUninstalledDependencies(FixFirstCombinedEntity entity, FixEntity fix)
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
        public List<FixEntity> GetDependentFixes(List<FixEntity> fixes, Guid guid)
        {
            var result = fixes.Where(x => x.Dependencies.Contains(guid)).ToList();

            return result;
        }

        /// <summary>
        /// Does fix have dependent fixes that are currently installed
        /// </summary>
        /// <param name="fixes">List of fix entities</param>
        /// <param name="guid">Guid of a fix</param>
        /// <returns>true if there are installed dependent fixes</returns>
        public bool DoesFixHaveInstalledDependentFixes(List<FixEntity> fixes, Guid guid)
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
        /// Uninstall fix
        /// </summary>
        /// <param name="game">Game entity</param>
        /// <param name="fix">Fix to delete</param>
        /// <returns>Result message</returns>
        public string UninstallFix(GameEntity game, FixEntity fix)
        {
            FixUninstaller.UninstallFix(game, fix);

            fix.InstalledFix = null;

            var result = InstalledFixesProvider.SaveInstalledFixes(_combinedEntitiesList);

            if (result.Item1)
            {
                return "Fix uninstalled successfully!";
            }
            else
            {
                return result.Item2;
            }
        }

        /// <summary>
        /// Install fix
        /// </summary>
        /// <param name="game">Game entity</param>
        /// <param name="fix">Fix to install</param>
        /// <returns>Result message</returns>
        public async Task<string> InstallFix(GameEntity game, FixEntity fix, string? variant)
        {
            InstalledFixEntity? installedFix = await FixInstaller.InstallFix(game, fix, variant);

            fix.InstalledFix = installedFix;

            var result = InstalledFixesProvider.SaveInstalledFixes(_combinedEntitiesList);

            if (result.Item1)
            {
                return "Fix installed successfully!";
            }
            else
            {
                return result.Item2;
            }
        }

        /// <summary>
        /// Update fix
        /// </summary>
        /// <param name="game">Game entity</param>
        /// <param name="fix">Fix to update</param>
        /// <returns>Result message</returns>
        public async Task<string> UpdateFix(GameEntity game, FixEntity fix, string? variant)
        {
            FixUninstaller.UninstallFix(game, fix);

            fix.InstalledFix = null;

            var installedFix = await FixInstaller.InstallFix(game, fix, variant);

            fix.InstalledFix = installedFix;

            var result = InstalledFixesProvider.SaveInstalledFixes(_combinedEntitiesList);

            if (result.Item1)
            {
                return "Fix updated successfully!";
            }
            else
            {
                return result.Item2;
            }
        }
    }
}
