using Common.Client.Logger;
using Common.Client.Providers.Interfaces;
using Common.Entities;
using CommunityToolkit.Diagnostics;
using System.Collections.Immutable;

namespace Common.Client.Providers;

public sealed class GamesProvider : IGamesProvider
{
    private readonly SteamTools _steamTools;
    private readonly ILogger _logger;
    private readonly SemaphoreSlim _semaphore = new(1);

    private ImmutableList<GameEntity>? _cache;


    public GamesProvider(
        SteamTools steamTools,
        ILogger logger
        )
    {
        _steamTools = steamTools;
        _logger = logger;
    }


    /// <inheritdoc/>
    public async Task<ImmutableList<GameEntity>> GetGamesListAsync(bool dropCache)
    {
        if (_cache is not null && !dropCache)
        {
            return _cache;
        }

        try
        {
            await _semaphore.WaitAsync().ConfigureAwait(false);

            await UpdateCacheAsync().ConfigureAwait(false);

            Guard.IsNotNull(_cache);

            return _cache;
        }
        finally 
        { 
            _ = _semaphore.Release(); 
        }
    }


    /// <summary>
    /// Update games cache
    /// </summary>
    /// <returns></returns>
    private async Task UpdateCacheAsync()
    {
        _logger.Info("Creating games cache list");

        var list = await Task.Run(() =>
        {
            var files = _steamTools.GetAcfsList();

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

            var cache = result.OrderBy(static x => x.Name).ToImmutableList();

            return cache;
        }).ConfigureAwait(false);

        _logger.Info($"Added {list.Count} games to the cache");

        _cache = list;
    }

    /// <summary>
    /// Parse ACF file to GameEntity
    /// </summary>
    /// <param name="file">Path to ACF file</param>
    private GameEntity? GetGameEntityFromAcf(string file)
    {
        var libraryFolder = Path.GetDirectoryName(file) ?? ThrowHelper.ThrowInvalidDataException<string>("Can't find install dir");

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
            else if (line.Contains("\"name\""))
            {
                var l = line.Split('"');

                name = l.ElementAt(l.Length - 2).Trim();
            }
            else if (line.Contains("\"installdir\""))
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

            var icon = _steamTools.SteamInstallPath is null
                ? string.Empty
                : Path.Combine(
                    _steamTools.SteamInstallPath,
                    Path.Combine("appcache", "librarycache", $"{id}_icon.jpg")
                    );

            return new GameEntity()
            {
                Id = id,
                Name = name,
                InstallDir = dir,
                Icon = icon
            };
        }

        return null;
    }
}

