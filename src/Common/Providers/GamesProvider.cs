using Common.Entities;
using System.Collections.Immutable;

namespace Common.Providers
{
    public sealed class GamesProvider
    {
        private bool _isCacheUpdating;
        private ImmutableList<GameEntity>? _gamesCache;

        /// <inheritdoc/>
        public async Task<ImmutableList<GameEntity>> GetCachedListAsync()
        {
            while (_isCacheUpdating)
            {
                await Task.Delay(100);
            }

            return _gamesCache ?? await GetNewListAsync();
        }

        /// <inheritdoc/>
        public async Task<ImmutableList<GameEntity>> GetNewListAsync()
        {
            while (_isCacheUpdating)
            {
                await Task.Delay(100);
            }

            _gamesCache = await CreateCacheAsync();

            return _gamesCache;
        }

        /// <inheritdoc/>
        private async Task<ImmutableList<GameEntity>> CreateCacheAsync()
        {
            _isCacheUpdating = true;

            List<GameEntity> result = new();

            await Task.Run(() =>
            {
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
            });

            _gamesCache = result.OrderBy(x => x.Name).ToImmutableList();

            _isCacheUpdating = false;

            return _gamesCache;
        }

        /// <summary>
        /// Parse ACF file to GameEntity
        /// </summary>
        /// <param name="file">Path to ACF file</param>
        private GameEntity? GetGameEntityFromAcf(string file)
        {
            var libraryFolder = Path.GetDirectoryName(file) ?? throw new Exception("Can't find install dir");

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
                if (!dir.EndsWith("\\") &&
                    !dir.EndsWith("/"))
                {
                    dir += Path.DirectorySeparatorChar;
                }

                return new GameEntity(id, name, dir);
            }

            return null;
        }
    }
}