using Common.Client.Logger;
using Common.Client.Providers.Interfaces;
using Common.Entities;
using Common.Entities.Fixes;
using Common.Helpers;
using CommunityToolkit.Diagnostics;
using System.Collections.Immutable;
using System.Text.Json;
using System.Threading;

namespace Common.Client.Providers;

public sealed class InstalledFixesProvider : IInstalledFixesProvider
{
    private readonly ILogger _logger;
    private readonly IGamesProvider _gamesProvider;
    private readonly SemaphoreSlim _semaphore = new(1);

    private List<BaseInstalledFixEntity>? _cache;


    public InstalledFixesProvider(
        IGamesProvider gamesProvider,
        ILogger logger
        )
    {
        _gamesProvider = gamesProvider;
        _logger = logger;
    }


    /// <inheritdoc/>
    public async Task<ImmutableList<BaseInstalledFixEntity>> GetInstalledFixesListAsync()
    {
        if (_cache is not null)
        {
            return [.. _cache];
        }

        try
        {
            await _semaphore.WaitAsync().ConfigureAwait(false);

            await UpdateCacheAsync().ConfigureAwait(false);

            Guard.IsNotNull(_cache);

            return [.. _cache];
        }
        finally
        {
            _ = _semaphore.Release();
        }
    }

    /// <inheritdoc/>
    public Result CreateInstalledJson(
        GameEntity game,
        BaseInstalledFixEntity installedFix
        )
    {
        Guard.IsNotNull(_cache);

        _cache.Add(installedFix);

        try
        {
            if (!Directory.Exists(Path.Combine(game.InstallDir, Consts.BackupFolder)))
            {
                _ = Directory.CreateDirectory(Path.Combine(game.InstallDir, Consts.BackupFolder));
            }

            var jsonText = JsonSerializer.Serialize(installedFix, InstalledFixesListContext.Default.BaseInstalledFixEntity);

            File.WriteAllText(Path.Combine(game.InstallDir, Consts.BackupFolder, installedFix.Guid.ToString() + ".json"), jsonText);
        }
        catch (Exception ex)
        {
            return new(ResultEnum.Error, ex.Message);
        }

        return new(ResultEnum.Success, string.Empty);
    }

    /// <inheritdoc/>
    public Result RemoveInstalledJson(
        GameEntity game, 
        Guid fixGuid
        )
    {
        Guard.IsNotNull(_cache);

        var toRemove = _cache.First(x => x.GameId == game.Id && x.Guid == fixGuid);
        _ = _cache.Remove(toRemove);

        try
        {
            File.Delete(Path.Combine(game.InstallDir, Consts.BackupFolder, fixGuid.ToString() + ".json"));
        }
        catch (Exception ex)
        {
            return new(ResultEnum.Error, ex.Message);
        }

        return new(ResultEnum.Success, string.Empty);
    }


    /// <summary>
    /// Update installed fixes cache
    /// </summary>
    private async Task UpdateCacheAsync()
    {
        _logger.Info("Requesting installed fixes");

        _cache = [];

        var games = await _gamesProvider.GetGamesListAsync(false).ConfigureAwait(false);

        foreach (var gameInstallDir in games.Select(static x => x.InstallDir).Distinct())
        {
            var superheaterFolder = Path.Combine(gameInstallDir, Consts.BackupFolder);

            if (!Directory.Exists(superheaterFolder))
            {
                continue;
            }

            var jsons = Directory.GetFiles(superheaterFolder, "*.json", SearchOption.TopDirectoryOnly);

            foreach (var json in jsons)
            {
                var jsonText = File.ReadAllText(json);

                var installedFixes = JsonSerializer.Deserialize(jsonText, InstalledFixesListContext.Default.BaseInstalledFixEntity);

                if (installedFixes is null)
                {
                    continue;
                }

                _cache.Add(installedFixes);
            }
        }
    }
}

