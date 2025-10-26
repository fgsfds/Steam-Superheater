using System.Collections.Immutable;
using System.Text.Json;
using Common.Axiom;
using Common.Axiom.Entities;
using Common.Axiom.Entities.Fixes;
using Common.Axiom.Helpers;
using Common.Client.Providers.Interfaces;
using CommunityToolkit.Diagnostics;
using Microsoft.Extensions.Logging;

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
    public async ValueTask<ImmutableList<BaseInstalledFixEntity>> GetInstalledFixesListAsync()
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
        _logger.LogInformation("Requesting installed fixes");

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

                BaseInstalledFixEntity? installedFixes;

                try
                {
                    installedFixes = JsonSerializer.Deserialize(jsonText, InstalledFixesListContext.Default.BaseInstalledFixEntity);
                }
                catch
                {
                    var jsonLines = File.ReadAllLines(json);

                    if (jsonText.Contains("VersionStr"))
                    {
                        for (var i = 0; i < jsonLines.Length; i++)
                        {
                            var line = jsonLines[i];
                            if (line.Contains(@"""Version"":"))
                            {
                                jsonLines[i] = string.Empty;
                            }
                        }

                        for (var i = 0; i < jsonLines.Length; i++)
                        {
                            if (jsonLines[i].Contains(@"""VersionStr"":"))
                            {
                                if (jsonLines[i].Contains("null"))
                                {
                                    jsonLines[i] = jsonLines[i].Replace(@"""VersionStr"": null", @"""Version"": ""1.0""");
                                }
                                else
                                {
                                    jsonLines[i] = jsonLines[i].Replace("VersionStr", "Version");
                                }
                            }
                        }

                        jsonText = string.Join(Environment.NewLine, jsonLines);
                    }
                    else
                    {
                        for (var i = 0; i < jsonLines.Length; i++)
                        {
                            var line = jsonLines[i];
                            if (line.Contains(@"""Version"":"))
                            {
                                jsonLines[i] = @"""Version"": ""1.0""";
                            }
                        }

                        jsonText = string.Join(Environment.NewLine, jsonLines);
                    }

                    if (jsonText.Contains("RegistryFixV2"))
                    {
                        jsonText = jsonText.Replace("RegistryFixV2", "RegistryFix");
                    }

                    try
                    {
                        installedFixes = JsonSerializer.Deserialize(jsonText, InstalledFixesListContext.Default.BaseInstalledFixEntity);

                        var instStr = JsonSerializer.Serialize(installedFixes!, InstalledFixesListContext.Default.BaseInstalledFixEntity);
                        File.WriteAllText(json, instStr);
                    }
                    catch
                    {
                        File.Delete(json);
                        installedFixes = null;
                    }
                }

                if (installedFixes is null)
                {
                    continue;
                }

                _cache.Add(installedFixes);
            }
        }
    }
}

