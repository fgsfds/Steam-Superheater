using Common.Entities;
using Common.Helpers;
using System.Collections.Immutable;

namespace Common.Providers
{
    public sealed class GamesProvider
    {
        private ImmutableList<GameEntity>? _gamesCache;
        private readonly SemaphoreSlim _locker = new(1);
        
        public async Task<ImmutableList<GameEntity>> GetListAsync(bool useCache) =>
            useCache
            ? await GetCachedListAsync()
            : await GetNewListAsync();

        /// <summary>
        /// Get cached games list from online or local repo or create new cache if it wasn't created yet
        /// </summary>
        private async Task<ImmutableList<GameEntity>> GetCachedListAsync()
        {
            Logger.Info("Requesting cached games list");

            await _locker.WaitAsync();

            var result = _gamesCache ?? CreateCache();

            _locker.Release();

            return result;
        }

        /// <summary>
        /// Remove current cache, then create new one and return games list
        /// </summary>
        private Task<ImmutableList<GameEntity>> GetNewListAsync()
        {
            Logger.Info("Requesting new games list");

            _gamesCache = null;

            return GetCachedListAsync();
        }

        /// <summary>
        /// Create new cache of games from online or local repository
        /// </summary>
        private ImmutableList<GameEntity> CreateCache()
        {
            Logger.Info("Creating games cache list");

            var files = SteamTools.GetAcfsList();

            List<GameEntity> result = new(files.Count);

            foreach (var file in files)
            {
                var games = GetGameEntityFromAcf(file);

                if (games is null)
                {
                    continue;
                }

                result.Add(games);
            }

            _gamesCache = [.. result.OrderBy(static x => x.Name)];

            Logger.Info($"Added {_gamesCache.Count} games to the cache");

            return _gamesCache;
        }

        /// <summary>
        /// Parse ACF file to GameEntity
        /// </summary>
        /// <param name="file">Path to ACF file</param>
        private static GameEntity? GetGameEntityFromAcf(string file)
        {
            var libraryFolder = Path.GetDirectoryName(file) ?? ThrowHelper.Exception<string>("Can't find install dir");

            var lines = File.ReadAllLines(file);

            var id = -1;
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
                if (!dir.EndsWith('\\') &&
                    !dir.EndsWith('/'))
                {
                    dir += Path.DirectorySeparatorChar;
                }

                return new GameEntity()
                {
                    Id = id,
                    Name = name,
                    InstallDir = dir,
                };
            }

            return null;
        }
    }
}