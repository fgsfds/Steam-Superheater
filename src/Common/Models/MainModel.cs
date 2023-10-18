using Common.CombinedEntities;
using Common.Config;
using Common.Entities;
using Common.Enums;
using Common.FixTools;
using Common.Providers;
using System.Collections.Immutable;

namespace Common.Models
{
    public sealed class MainModel
    {
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

        private readonly ConfigEntity _config;
        private readonly InstalledFixesProvider _installedFixesProvider;
        private readonly CombinedEntitiesProvider _combinedEntitiesProvider;
        private readonly FixInstaller _fixInstaller;
        private readonly FixUninstaller _fixUninstaller;

        private readonly List<FixFirstCombinedEntity> _combinedEntitiesList;

        public int UpdateableGamesCount => _combinedEntitiesList.Count(x => x.HasUpdates);

        public bool HasUpdateableGames => UpdateableGamesCount > 0;

        /// <summary>
        /// Update list of games either from cache or by downloading fixes.xml from repo
        /// </summary>
        /// <param name="useCache">Is cache used</param>
        public async Task<Tuple<bool, string>> UpdateGamesListAsync(bool useCache)
        {
            _combinedEntitiesList.Clear();

            try
            {
                var games = await _combinedEntitiesProvider.GetFixFirstEntitiesAsync(useCache);
                var hiddenTags = _config.HiddenTags;

                foreach (var game in games.ToList())
                {
                    foreach (var fix in game.FixesList.Fixes.ToList())
                    {
                        if (fix.Tags is not null &&
                            fix.Tags.Any(x => hiddenTags.Contains(x)))
                        {
                            game.FixesList.Fixes.Remove(fix);
                        }
                    }

                    if (!game.FixesList.Fixes.Any())
                    {
                        games.Remove(game);
                    }
                }

                _combinedEntitiesList.AddRange(games);

                return new(true, string.Empty);
            }
            catch (Exception ex) when (ex is FileNotFoundException || ex is DirectoryNotFoundException)
            {
                return new(false, "File not found: " + ex.Message);
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
            {
                return new(false, "Can't connect to GitHub repository");
            }
        }

        public ImmutableList<FixEntity> GetFixesForSelectedGame(FixFirstCombinedEntity? game)
        {
            if (game is null)
            {
                return ImmutableList.Create<FixEntity>();
            }

            var list = game.FixesList.Fixes.ToImmutableList();

            if (_config.ShowUnsupportedFixes)
            {
                return list;
            }
            else
            {
                return list.Where(x => x.SupportedOSes.HasFlag(OSEnumHelper.GetCurrentOS())).ToImmutableList();
            }
        }

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
        /// Get list of games optionally filtered by a search string
        /// </summary>
        /// <param name="search">Search string</param>
        public ImmutableList<FixFirstCombinedEntity> GetFilteredGamesList(string? search = null)
        {
            List<FixFirstCombinedEntity> result = _combinedEntitiesList;

            if (!_config.ShowUninstalledGames)
            {
                result = _combinedEntitiesList.Where(x => x.IsGameInstalled).ToList();
            }

            if (string.IsNullOrEmpty(search))
            {
                return result.ToImmutableList();
            }

            return result.Where(x => x.GameName.ToLower().Contains(search.ToLower())).ToImmutableList();
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
        public List<FixEntity> GetDependentFixes(IEnumerable<FixEntity> fixes, Guid guid)
            => fixes.Where(x => x.Dependencies is not null && x.Dependencies.Contains(guid)).ToList();

        /// <summary>
        /// Does fix have dependent fixes that are currently installed
        /// </summary>
        /// <param name="fixes">List of fix entities</param>
        /// <param name="guid">Guid of a fix</param>
        /// <returns>true if there are installed dependent fixes</returns>
        public bool DoesFixHaveInstalledDependentFixes(IEnumerable<FixEntity> fixes, Guid guid)
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
        public Tuple<bool, string> UninstallFix(GameEntity game, FixEntity fix)
        {
            _fixUninstaller.UninstallFix(game, fix);

            fix.InstalledFix = null;

            var result = _installedFixesProvider.SaveInstalledFixes(_combinedEntitiesList);

            if (result.Item1)
            {
                return new(true, "Fix uninstalled successfully!");
            }
            else
            {
                return new(false, result.Item2);
            }
        }

        /// <summary>
        /// Install fix
        /// </summary>
        /// <param name="game">Game entity</param>
        /// <param name="fix">Fix to install</param>
        /// <returns>Result message</returns>
        public async Task<Tuple<bool, string>> InstallFix(GameEntity game, FixEntity fix, string? variant)
        {
            InstalledFixEntity? installedFix;

            try
            {
                installedFix = await _fixInstaller.InstallFix(game, fix, variant);
            }
            catch (Exception ex)
            {
                return new(false, "Error while downloading fix: " + Environment.NewLine + Environment.NewLine + ex.Message);
            }

            fix.InstalledFix = installedFix;

            var result = _installedFixesProvider.SaveInstalledFixes(_combinedEntitiesList);

            if (result.Item1)
            {
                return new(true, "Fix installed successfully!");
            }
            else
            {
                return new(false, result.Item2);
            }
        }

        /// <summary>
        /// Update fix
        /// </summary>
        /// <param name="game">Game entity</param>
        /// <param name="fix">Fix to update</param>
        /// <returns>Result message</returns>
        public async Task<Tuple<bool, string>> UpdateFix(GameEntity game, FixEntity fix, string? variant)
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
                return new(false, "Error while downloading fix: " + Environment.NewLine + Environment.NewLine + ex.Message);
            }

            fix.InstalledFix = installedFix;

            var result = _installedFixesProvider.SaveInstalledFixes(_combinedEntitiesList);

            if (result.Item1)
            {
                return new(true, "Fix updated successfully!");
            }
            else
            {
                return new(false, result.Item2);
            }
        }
    }
}
