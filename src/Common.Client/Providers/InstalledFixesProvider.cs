using Common.Entities;
using Common.Entities.Fixes;
using Common.Helpers;
using System.Collections.Immutable;
using System.Text.Json;

namespace Common.Client.Providers
{
    public sealed class InstalledFixesProvider
    {
        private readonly Logger _logger;
        private readonly GamesProvider _gamesProvider;

        private List<BaseInstalledFixEntity>? _cache;


        public InstalledFixesProvider(
            GamesProvider gamesProvider,
            Logger logger
            )
        {
            _gamesProvider = gamesProvider;
            _logger = logger;
        }


        public async Task<ImmutableList<BaseInstalledFixEntity>> GetInstalledFixesListAsync()
        {
            if (_cache is not null)
            {
                return [.. _cache];
            }

            await CreateCacheAsync().ConfigureAwait(false);

            _cache.ThrowIfNull();

            return [.. _cache];
        }

        /// <summary>
        /// Add installed fix to cache
        /// </summary>
        /// <param name="installedFix">Installed fix entity</param>
        public Result CreateInstalledJson(GameEntity game, BaseInstalledFixEntity installedFix)
        {
            _cache.ThrowIfNull();

            _cache.Add(installedFix);

            try
            {
                if (!Directory.Exists(Path.Combine(game.InstallDir, Consts.BackupFolder)))
                {
                    Directory.CreateDirectory(Path.Combine(game.InstallDir, Consts.BackupFolder));
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

        /// <summary>
        /// Remove installed fix from cache
        /// </summary>
        /// <param name="game">Game</param>
        /// <param name="fixGuid">Fix guid</param>
        public Result RemoveInstalledJson(GameEntity game, Guid fixGuid)
        {
            _cache.ThrowIfNull();

            var toRemove = _cache.First(x => x.GameId == game.Id && x.Guid == fixGuid);
            _cache.Remove(toRemove);

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
        /// Create installed fixes cache
        /// </summary>
        private async Task CreateCacheAsync()
        {
            _logger.Info("Requesting installed fixes");

            _cache = [];

            var games = await _gamesProvider.GetGamesListAsync().ConfigureAwait(false);

            //TODO remove in 1.0
            if (File.Exists(Consts.InstalledFile))
            {
                foreach (var game in games)
                {
                    var sfdFolder = Path.Combine(game.InstallDir, ".sfd");

                    if (Directory.Exists(sfdFolder))
                    {
                        Directory.Move(sfdFolder, Path.Combine(game.InstallDir, Consts.BackupFolder));
                    }
                }

                await ConvertOldInstalledJsonAsync(games).ConfigureAwait(false);
            }

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

        /// <summary>
        /// Convert old installed.json to a new format
        /// </summary>
        [Obsolete("Remove in version 1.0")]
        private async Task ConvertOldInstalledJsonAsync(IEnumerable<GameEntity> games)
        {
            var text = await File.ReadAllTextAsync(Consts.InstalledFile).ConfigureAwait(false);

            text.ThrowIfNull();

            var installedFixes = JsonSerializer.Deserialize(text, InstalledFixesListContext.Default.ListBaseInstalledFixEntity);

            installedFixes.ThrowIfNull();

            _ = FixWrongGuids(installedFixes);

            SaveInstalledFixes(installedFixes, games);

            File.Delete(Consts.InstalledFile);
        }

        /// <summary>
        /// Save installed fixes to XML
        /// </summary>
        [Obsolete("Remove in version 1.0")]
        private Result SaveInstalledFixes(List<BaseInstalledFixEntity> installedFixes, IEnumerable<GameEntity> games)
        {
            try
            {
                foreach (var installedFix in installedFixes)
                {
                    var json = JsonSerializer.Serialize(
                    installedFix,
                    InstalledFixesListContext.Default.BaseInstalledFixEntity
                    );

                    var game = games.FirstOrDefault(x => x.Id == installedFix.GameId);

                    if (game is null)
                    {
                        continue;
                    }

                    File.WriteAllText(Path.Combine(game.InstallDir, Consts.BackupFolder, installedFix.Guid.ToString() + ".json"), json);
                }

                return new(ResultEnum.Success, string.Empty);
            }
            catch (Exception ex) when (ex is FileNotFoundException || ex is DirectoryNotFoundException)
            {
                _logger.Error(ex.ToString());
                return new Result(ResultEnum.NotFound, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.Error(ex.ToString());
                return new Result(ResultEnum.Error, ex.Message);
            }
        }

        /// <summary>
        /// Fix wrong guids in installed fixes
        /// </summary>
        [Obsolete("Remove in version 1.0")]
        private bool FixWrongGuids(List<BaseInstalledFixEntity> installedFixes)
        {
            var needToSave = false;

            foreach (var fix in installedFixes)
            {
                if (fix.GameId == 299030 &&
                    fix.Guid == new Guid("8bd92099-4d55-47fb-961d-033ab3cb5570"))
                {
                    fix.Guid = new("be21caea-ccab-47a6-979e-c47b73ef0a43");

                    needToSave = true;
                }
                else if (fix.GameId == 299030 &&
                    fix.Guid == new Guid("40d4fed2-060f-46f8-9977-f1dd7c26a868"))
                {
                    fix.Guid = new("09d1013a-75ce-4879-b7ca-ae155df63424");

                    needToSave = true;
                }
                else if (fix.GameId == 108710 &&
                    fix.Guid == new Guid("e2cfbadc-fe21-4898-bf47-e6a9c7f784d4"))
                {
                    fix.Guid = new("e026709c-085c-4cfe-9120-9bbf09109270");

                    needToSave = true;
                }
                else if (fix.GameId == 282900 &&
                    fix.Guid == new Guid("e2cfbadc-fe21-4898-bf47-e6a9c7f784d4"))
                {
                    fix.Guid = new("7fc5eefe-7436-42f3-af33-b7747817c333");

                    needToSave = true;
                }
                else if (fix.GameId == 351710 &&
                    fix.Guid == new Guid("e2cfbadc-fe21-4898-bf47-e6a9c7f784d4"))
                {
                    fix.Guid = new("9c338265-1f5c-4618-a6df-36601eca069e");

                    needToSave = true;
                }
                else if (fix.GameId == 353270 &&
                    fix.Guid == new Guid("e2cfbadc-fe21-4898-bf47-e6a9c7f784d4"))
                {
                    fix.Guid = new("a8544f69-d32a-4912-b8d1-e72883a2d0c3");

                    needToSave = true;
                }
            }

            return needToSave;
        }
    }
}
