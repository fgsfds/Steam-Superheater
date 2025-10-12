using System.Collections.Immutable;
using Common.Client.Providers.Interfaces;
using Common.Entities;
using CommunityToolkit.Diagnostics;
using Microsoft.Extensions.Logging;

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
    public async ValueTask<ImmutableList<GameEntity>> GetGamesListAsync(bool dropCache)
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
        _logger.LogInformation("Creating games cache list");

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

        _logger.LogInformation($"Added {list.Count} games to the cache");

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
        uint buildId = 0;
        uint targetBuildId = 0;

        foreach (var line in lines)
        {
            if (line.Contains("\"appid\"", StringComparison.OrdinalIgnoreCase))
            {
                var l = line.Split('"');

                var z = l.ElementAt(l.Length - 2).Trim();

                _ = int.TryParse(z, out id);
            }
            else if (line.Contains("\"name\"", StringComparison.OrdinalIgnoreCase))
            {
                var l = line.Split('"');

                name = l.ElementAt(l.Length - 2).Trim();
            }
            else if (line.Contains("\"installdir\"", StringComparison.OrdinalIgnoreCase))
            {
                var l = line.Split('"');

                dir = Path.Combine(libraryFolder, "common", l.ElementAt(l.Length - 2).Trim());
            }
            else if (line.Contains("\"buildid\"", StringComparison.OrdinalIgnoreCase))
            {
                var l = line.Split('"');

                _ = uint.TryParse(l.ElementAt(l.Length - 2).Trim(), out buildId);
            }
            else if (line.Contains("\"TargetBuildID\"", StringComparison.OrdinalIgnoreCase))
            {
                var l = line.Split('"');

                _ = uint.TryParse(l.ElementAt(l.Length - 2).Trim(), out targetBuildId);
            }
        }

        if (!string.IsNullOrEmpty(dir) && !string.IsNullOrEmpty(name))
        {
            if (!dir.EndsWith('\\') &&
                !dir.EndsWith('/'))
            {
                dir += Path.DirectorySeparatorChar;
            }

            string icon = string.Empty;

            if (_steamTools.SteamInstallPath is not null)
            {
                var ico = Path.Combine(_steamTools.SteamInstallPath, "appcache", "librarycache", $"{id}_icon.jpg");
                var lib = Path.Combine(_steamTools.SteamInstallPath, "appcache", "librarycache", id.ToString());
                if (File.Exists(ico))
                {
                    icon = ico;
                }
                else if (Directory.Exists(lib))
                {
                    var images = Directory.GetFiles(Path.Combine(_steamTools.SteamInstallPath, "appcache", "librarycache", id.ToString())).OrderBy(x => x.Length);

                    if (images.Any())
                    {
                        icon = images.Last();
                    }
                }
            }

            return new GameEntity()
            {
                Id = id,
                Name = name,
                InstallDir = dir,
                Icon = icon,
                BuildId = buildId,
                TargetBuildId = targetBuildId,
            };
        }

        return null;
    }
}

