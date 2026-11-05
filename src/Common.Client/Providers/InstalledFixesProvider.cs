using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Nodes;
using Common.Axiom;
using Common.Axiom.Entities;
using Common.Axiom.Entities.Fixes;
using Common.Client.Providers.Interfaces;
using Microsoft.Extensions.Logging;

namespace Common.Client.Providers;

/// <summary>
/// Persists installed records before publishing synchronized cache changes.
/// </summary>
public sealed class InstalledFixesProvider : IInstalledFixesProvider, IDisposable
{
    private readonly ILogger _logger;
    private readonly IGamesProvider _gamesProvider;
    private readonly SemaphoreSlim _semaphore = new(1);
    private List<BaseInstalledFixEntity>? _cache;

    /// <summary>
    /// Creates the provider for the discovered game installations.
    /// </summary>
    public InstalledFixesProvider(IGamesProvider gamesProvider, ILogger logger)
    {
        _gamesProvider = gamesProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<ImmutableList<BaseInstalledFixEntity>> GetInstalledFixesListAsync()
    {
        await _semaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_cache is null)
            {
                _cache = await LoadRecordsAsync().ConfigureAwait(false);
            }

            return [.. _cache];
        }
        finally
        {
            _ = _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public Result CreateInstalledJson(GameEntity game, BaseInstalledFixEntity installedFix)
    {
        _semaphore.Wait();
        try
        {
            var path = Path.Combine(game.InstallDir, ClientConstants.BackupFolder, installedFix.Guid + ".json");
            WriteRecord(path, installedFix);
            if (_cache is not null)
            {
                _ = _cache.RemoveAll(x => x.GameId == game.Id && x.Guid == installedFix.Guid);
                _cache.Add(installedFix);
            }

            return new(ResultEnum.Success, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to save installed fix {Fix}", installedFix.Guid);
            return new(ResultEnum.Error, ex.Message);
        }
        finally
        {
            _ = _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public Result RemoveInstalledJson(GameEntity game, Guid fixGuid)
    {
        _semaphore.Wait();
        try
        {
            File.Delete(Path.Combine(game.InstallDir, ClientConstants.BackupFolder, fixGuid + ".json"));
            if (_cache is not null)
            {
                _ = _cache.RemoveAll(x => x.GameId == game.Id && x.Guid == fixGuid);
            }

            return new(ResultEnum.Success, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to remove installed fix {Fix}", fixGuid);
            return new(ResultEnum.Error, ex.Message);
        }
        finally
        {
            _ = _semaphore.Release();
        }
    }

    private async Task<List<BaseInstalledFixEntity>> LoadRecordsAsync()
    {
        var records = new List<BaseInstalledFixEntity>();
        var games = await _gamesProvider.GetGamesListAsync(false).ConfigureAwait(false);
        foreach (var gameDirectory in games.Select(x => x.InstallDir).Distinct())
        {
            Common.Client.FixTools.FileFix.FileInstallTransaction.Recover(gameDirectory);
            var directory = Path.Combine(gameDirectory, ClientConstants.BackupFolder);
            if (!Directory.Exists(directory))
            {
                continue;
            }

            foreach (var path in Directory.GetFiles(directory, "*.json"))
            {
                try
                {
                    var text = await File.ReadAllTextAsync(path).ConfigureAwait(false);
                    BaseInstalledFixEntity record;
                    try
                    {
                        record = JsonSerializer.Deserialize(text, InstalledFixesListContext.Default.BaseInstalledFixEntity)
                            ?? throw new JsonException("Installed record is null.");
                    }
                    catch (JsonException)
                    {
                        record = ReadLegacyRecord(text);
                        File.Copy(path, path + ".legacy-" + Guid.NewGuid().ToString("N"));
                        WriteRecord(path, record);
                    }

                    records.Add(record);
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Quarantining unreadable installed record {Path}", path);
                    try
                    {
                        File.Move(path, path + ".corrupt-" + Guid.NewGuid().ToString("N"));
                    }
                    catch (IOException moveError)
                    {
                        _logger.LogError(moveError, "Keeping unreadable record at {Path}", path);
                    }
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    _logger.LogError(ex, "Preserving inaccessible installed record {Path}", path);
                }
            }
        }

        return records;
    }

    private static BaseInstalledFixEntity ReadLegacyRecord(string text)
    {
        var node = JsonNode.Parse(text) as JsonObject ?? throw new JsonException("Expected installed record object.");
        if (node["VersionStr"] is not null)
        {
            node["Version"] = node["VersionStr"]!.DeepClone();
            _ = node.Remove("VersionStr");
        }
        else if (node["Version"] is not JsonValue value || !value.TryGetValue<string>(out _))
        {
            node["Version"] = "1.0";
            _ = node.Remove("VersionStr");
        }

        if (node["$type"]?.GetValue<string>() == "RegistryFixV2")
        {
            node["$type"] = "RegistryFix";
        }

        return node.Deserialize(InstalledFixesListContext.Default.BaseInstalledFixEntity)
            ?? throw new JsonException("Installed record is null.");
    }

    internal static void WriteRecord(string path, BaseInstalledFixEntity record)
    {
        _ = Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temporary = path + ".tmp-" + Guid.NewGuid().ToString("N");
        try
        {
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                JsonSerializer.Serialize(stream, record, InstalledFixesListContext.Default.BaseInstalledFixEntity);
                stream.Flush(true);
            }

            File.Move(temporary, path, true);
        }
        finally
        {
            if (File.Exists(temporary))
            {
                File.Delete(temporary);
            }
        }
    }

    /// <inheritdoc />
    public void InvalidateCache()
    {
        _semaphore.Wait();
        try { _cache = null; }
        finally { _ = _semaphore.Release(); }
    }

    /// <inheritdoc />
    public void Dispose() => _semaphore.Dispose();
}
