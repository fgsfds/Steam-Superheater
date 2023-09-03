using FluentFTP;
using SteamFDCommon.DI;
using SteamFDCommon.Entities;
using SteamFDCommon.Providers;
using System.Collections.ObjectModel;
using System.Net;
using System.Xml.Serialization;

namespace SteamFDCommon.Models
{
    public class EditorModel
    {
        private readonly SortedList<string, FixesList> _fixesList;
        private readonly FixesProvider _fixesProvider;

        public EditorModel(FixesProvider fixesProvider)
        {
            _fixesProvider = fixesProvider ?? throw new NullReferenceException(nameof(fixesProvider));
            _fixesList = new();
        }

        /// <summary>
        /// Update list of fixes either from cache or by downloading fixes.xml from repo
        /// </summary>
        /// <param name="useCache">Is cache used</param>
        public async Task UpdateFixesListAsync(bool useCache)
        {
            _fixesList.Clear();

            var fixes = await CombinedEntitiesProvider.GetFixFirstEntitiesAsync(useCache);

            fixes = fixes.OrderBy(x => x.GameName).ToList();

            foreach (var fix in fixes)
            {
                _fixesList.Add(fix.GameName, fix);
            }
        }

        /// <summary>
        /// Get list of fixes optionally filtered by a search string
        /// </summary>
        /// <param name="search">Search string</param>
        public List<FixesList> GetFilteredFixesList(string? search = null)
        {
            List<FixesList> result = new();

            result = _fixesList.Select(x => x.Value).Where(x => x.Fixes.Count > 0).ToList();

            if (!string.IsNullOrEmpty(search))
            {
                result = result.Where(x => x.GameName.ToLower().Contains(search.ToLower())).ToList();
            }

            return result;
        }

        /// <summary>
        /// Get list of games that can be added to the fixes list
        /// </summary>
        public List<GameEntity> GetListOfGamesAvailableToAdd()
        {
            List<GameEntity> result = new();

            var installedGames = BindingsManager.Instance.GetInstance<GamesProvider>().GetCachedGamesList();

            foreach (var game in installedGames)
            {
                if (_fixesList.Any(x => x.Value.GameId == game.Id))
                {
                    continue;
                }

                result.Add(game);
            }

            return result;
        }

        /// <summary>
        /// Save current fixes list to XML
        /// </summary>
        /// <returns>Result message</returns>
        public Tuple<bool, string> SaveFixesListAsync()
        {
            var result = FixesProvider.SaveFixes(_fixesList.Select(x => x.Value).ToList());

            return result;
        }

        /// <summary>
        /// Get list of dependencies for a fix
        /// </summary>
        /// <param name="fixesList">Fixes list</param>
        /// <param name="fixEntity">Fix</param>
        /// <returns>List of dependencies</returns>
        public List<FixEntity> GetDependenciesForAFix(FixesList fixesList, FixEntity fixEntity)
        {
            if (fixEntity?.Dependencies is null)
            {
                return new List<FixEntity>();
            }

            var allGameFixes = _fixesList.Where(x => x.Value.GameId == fixesList.GameId).FirstOrDefault();

            if (allGameFixes.Value is null)
            {
                return new List<FixEntity>();
            }

            var allGameDeps = fixEntity.Dependencies;

            var deps = allGameFixes.Value.Fixes.Where(x => allGameDeps.Contains(x.Guid)).ToList();

            return deps;
        }

        /// <summary>
        /// Get list of fixes that can be added as dependencies
        /// </summary>
        /// <param name="fixesList">Fixes list</param>
        /// <param name="fixEntity">Fix</param>
        /// <returns>List of fixes</returns>
        public List<FixEntity> GetListOfDependenciesAvailableToAdd(FixesList fixesList, FixEntity fixEntity)
        {
            if (fixesList is null ||
                fixEntity is null)
            {
                return new List<FixEntity>();
            }

            List<FixEntity> result = new();

            var fixDependencies = GetDependenciesForAFix(fixesList, fixEntity);

            foreach (var fix in fixesList.Fixes)
            {

                if (
                    //don't add itself
                    fix.Guid != fixEntity.Guid &&
                    //don't add fixes that depend of it
                    !fix.Dependencies.Any(x => x == fixEntity.Guid) &&
                    //don't add fixes that are already dependencies
                    !fixDependencies.Where(x => x.Guid == fix.Guid).Any()
                    )
                {
                    result.Add(fix);
                }
            }

            return result;
        }

        public void AddDependencyForFix(FixEntity addTo, FixEntity dependency)
        {
            addTo.Dependencies.Add(dependency.Guid);
        }

        public void RemoveDependencyForFix(FixEntity addTo, FixEntity dependency)
        {
            addTo.Dependencies.Remove(dependency.Guid);
        }

        /// <summary>
        /// Add new fixes list for a game
        /// </summary>
        /// <param name="game">Game entity</param>
        public FixesList AddNewFix(GameEntity game)
        {
            var newFix = new FixesList(game.Id, game.Name, new ObservableCollection<FixEntity>());

            newFix.Fixes.Add(new FixEntity());

            _fixesList.Add(newFix.GameName, newFix);

            return newFix;
        }

        public async Task<bool> UploadFixAsync(FixesList newFix)
        {
            var onlineFixes = await _fixesProvider.GetOnlineFixesListAsync();

            var guid = newFix.Fixes.First().Guid;

            foreach (var onlineFix in onlineFixes)
            {
                //if fix already exists in the repo, don't upload it
                if (onlineFix.Fixes.Any(x => x.Guid == guid))
                {
                    return false;
                }
            }

            XmlSerializer xmlSerializer = new(typeof(FixesList));

            using (MemoryStream fs = new())
            {
                xmlSerializer.Serialize(fs, newFix);

                var client = new FtpClient("31.31.198.106", "u2220544_Upload", "YdBunW64d447Pby");

                client.CreateDirectory(guid.ToString());

                client.UploadStream(fs, $"{guid}/fix.xml");
            }

            return true;
        }
    }
}
