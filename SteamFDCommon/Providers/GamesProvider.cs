using SteamFDTCommon.Entities;
using SteamFDTools;

namespace SteamFDTCommon.Providers
{
    public class GamesProvider
    {
        private bool _isCacheUpdating;

        private List<GameEntity>? _gamesCache;

        /// <summary>
        /// Get cached games list or create new cache if it wasn't created yet
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NullReferenceException"></exception>
        public List<GameEntity> GetCachedGamesList()
        {
            while (_isCacheUpdating)
            {
                Thread.Sleep(100);
            }

            if (_gamesCache is null)
            {
                CreateGamesCache();
            }

            return _gamesCache ?? throw new NullReferenceException(nameof(_gamesCache));
        }

        /// <summary>
        /// Remove current cache, then create new one and return games list
        /// </summary>
        /// <returns></returns>
        public List<GameEntity> GetNewGamesList()
        {
            _gamesCache = null;

            return GetCachedGamesList();
        }

        /// <summary>
        /// Create new cache of installed games
        /// </summary>
        /// <returns></returns>
        private void CreateGamesCache()
        {
            _isCacheUpdating = true;

            List<GameEntity> result = new();

            var files = SteamTools.GetAcfsList();

            foreach (var file in files)
            {
                var games = GetGameEntityFromAcf(file);

                if (games is null)
                {
                    continue;
                }

                result.Add(games);
            }

            _gamesCache = result.OrderBy(x => x.Name).ToList();

            _isCacheUpdating = false;
        }

        /// <summary>
        /// Parse ACF file to GameEntity
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        private GameEntity? GetGameEntityFromAcf(string file)
        {
            var libraryFolder = Path.GetDirectoryName(file);

            var lines = File.ReadAllLines(file);

            int id = -1;
            string? name = null;
            string? dir = null;

            foreach (var line in lines)
            {
                if (line.Contains("\"appid\""))
                {
                    var l = line.Split('"');

                    var z = l.ElementAt(l.Length - 2).Trim();

                    _ = int.TryParse(z, out id);
                }
                if (line.Contains("\"name\""))
                {
                    var l = line.Split('"');

                    name = l.ElementAt(l.Length - 2).Trim();
                }
                if (line.Contains("\"installdir\""))
                {
                    var l = line.Split('"');

                    dir = Path.Combine(libraryFolder, "common", l.ElementAt(l.Length - 2).Trim());
                }
            }

            if (!string.IsNullOrEmpty(dir) && !string.IsNullOrEmpty(name))
            {
                return new GameEntity(id, name, dir);
            }

            return null;
        }
    }
}