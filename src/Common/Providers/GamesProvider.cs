using Common.Entities;
using System.Collections.Immutable;

namespace Common.Providers
{
    public sealed class GamesProvider
    {
        private ImmutableList<GameEntity>? _gamesCache;
        private readonly SemaphoreSlim _locker = new(1, 1);

        /// <inheritdoc/>
        public async Task<ImmutableList<GameEntity>> GetCachedListAsync()
        {
            await _locker.WaitAsync();

            var result = _gamesCache ?? await CreateCacheAsync();

            _locker.Release();

            return result;
        }

        /// <inheritdoc/>
        public async Task<ImmutableList<GameEntity>> GetNewListAsync()
        {
            _gamesCache = null;

            return await GetCachedListAsync();
        }

        /// <inheritdoc/>
        private async Task<ImmutableList<GameEntity>> CreateCacheAsync()
        {
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