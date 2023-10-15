using Common.CombinedEntities;
using Common.Config;
using Common.Entities;
using Common.FixTools;
using Common.Providers;

namespace Common.Models
{
    public sealed class MainModel
    {
        private readonly ConfigEntity _config;
        private readonly InstalledFixesProvider _installedFixesProvider;
        private readonly CombinedEntitiesProvider _combinedEntitiesProvider;
        private readonly FixInstaller _fixInstaller;
        private readonly FixUninstaller _fixUninstaller;

        private readonly List<FixFirstCombinedEntity> _combinedEntitiesList;

        public int UpdateableGamesCount => _combinedEntitiesList.Count(x => x.HasUpdates);

        public bool HasUpdateableGames => UpdateableGamesCount > 0;

        public MainModel(
            ConfigProvider configProvider,
            InstalledFixesProvider installedFixesProvider,
            CombinedEntitiesProvider combinedEntitiesProvider,
            FixInstaller fixInstaller,
            FixUninstaller fixUninstaller
            )
        {
            _combinedEntitiesList = new();
            _config = configProvider?.Config ?? throw new NullReferenceException(nameof(configProvider));
            _installedFixesProvider = installedFixesProvider ?? throw new NullReferenceException(nameof(installedFixesProvider));
            _combinedEntitiesProvider = combinedEntitiesProvider ?? throw new NullReferenceException(nameof(combinedEntitiesProvider));
            _fixInstaller = fixInstaller ?? throw new NullReferenceException(nameof(fixInstaller));
            _fixUninstaller = fixUninstaller ?? throw new NullReferenceException(nameof(fixUninstaller));
        }

        /// <summary>
        /// Update list of games either from cache or by downloading fixes.xml from repo
        /// </summary>
        /// <param name="useCache">Is cache used</param>
        public async Task UpdateGamesListAsync(bool useCache)
        {
            _combinedEntitiesList.Clear();

            var games = await _combinedEntitiesProvider.GetFixFirstEntitiesAsync(useCache);

            _combinedEntitiesList.AddRange(games);
        }

        /// <summary>
        /// Get list of games optionally filtered by a search string
        /// </summary>
        /// <param name="search">Search string</param>
        public List<FixFirstCombinedEntity> GetFilteredGamesList(string? search = null)
        {
            List<FixFirstCombinedEntity> result = _combinedEntitiesList;

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
            _fixUninstaller.UninstallFix(game, fix);

            fix.InstalledFix = null;

            var result = _installedFixesProvider.SaveInstalledFixes(_combinedEntitiesList);

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
            InstalledFixEntity? installedFix;

            try
            {
                installedFix = await _fixInstaller.InstallFix(game, fix, variant);
            }
            catch (Exception ex)
            {
                return "Error while downloading fix: " + Environment.NewLine + Environment.NewLine + ex.Message;
            }

            fix.InstalledFix = installedFix;

            var result = _installedFixesProvider.SaveInstalledFixes(_combinedEntitiesList);

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
            _fixUninstaller.UninstallFix(game, fix);

            fix.InstalledFix = null;

            InstalledFixEntity? installedFix;

            try
            {
                installedFix = await _fixInstaller.InstallFix(game, fix, variant);
            }
            catch (Exception ex)
            {
                return "Error while downloading fix: " + Environment.NewLine + Environment.NewLine + ex.Message;
            }

            fix.InstalledFix = installedFix;

            var result = _installedFixesProvider.SaveInstalledFixes(_combinedEntitiesList);

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
