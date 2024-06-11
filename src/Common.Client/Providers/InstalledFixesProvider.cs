using Common;
using Common.Entities.Fixes;
using Common.Entities.Fixes.FileFix;
using Common.Entities.Fixes.HostsFix;
using Common.Entities.Fixes.RegistryFix;
using Common.Enums;
using Common.Helpers;
using System.Collections.Immutable;
using System.Text.Json;
using System.Xml.Linq;

namespace Common.Client.Providers
{
    public sealed class InstalledFixesProvider
    {
        private readonly Logger _logger;
        private List<BaseInstalledFixEntity>? _cache;


        public InstalledFixesProvider(Logger logger)
        {
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
        /// Save installed fixes to XML
        /// </summary>
        /// <returns>Result struct</returns>
        public Result SaveInstalledFixes()
        {
            _logger.Info("Saving installed fixes list");

            try
            {
                _cache.ThrowIfNull();

                var json = JsonSerializer.Serialize(
                    _cache,
                    InstalledFixesListContext.Default.ListBaseInstalledFixEntity
                    );

                File.WriteAllText(Consts.InstalledFile, json);

                _logger.Info("Fixes list saved successfully");

                return new(ResultEnum.Success, string.Empty);
            }
            catch (Exception ex) when (ex is FileNotFoundException || ex is DirectoryNotFoundException)
            {
                _logger.Error(ex.Message);
                return new Result(ResultEnum.NotFound, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message);
                return new Result(ResultEnum.Error, ex.Message);
            }
        }

        /// <summary>
        /// Add installed fix to cache
        /// </summary>
        /// <param name="installedFix">Installed fix entity</param>
        public void AddToCache(BaseInstalledFixEntity installedFix)
        {
            _cache.ThrowIfNull();

            _cache.Add(installedFix);
        }

        /// <summary>
        /// Remove installed fix from cache
        /// </summary>
        /// <param name="gameId">Game id</param>
        /// <param name="fixGuid">Fix guid</param>
        public void RemoveFromCache(int gameId, Guid fixGuid)
        {
            _cache.ThrowIfNull();

            var toRemove = _cache.First(x => x.GameId == gameId && x.Guid == fixGuid);
            _cache.Remove(toRemove);
        }

        /// <inheritdoc/>
        private async Task CreateCacheAsync()
        {
            _logger.Info("Requesting installed fixes");

            if (!File.Exists(Consts.InstalledFile))
            {
                _cache = [];
                return;
            }

            var text = await File.ReadAllTextAsync(Consts.InstalledFile).ConfigureAwait(false);

            text.ThrowIfNull();

            var installedFixes = JsonSerializer.Deserialize(text, InstalledFixesListContext.Default.ListBaseInstalledFixEntity);

            installedFixes.ThrowIfNull();

            var needToSave = FixWrongGuids(installedFixes);

            _cache = installedFixes;

            if (needToSave)
            {
                SaveInstalledFixes();
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
