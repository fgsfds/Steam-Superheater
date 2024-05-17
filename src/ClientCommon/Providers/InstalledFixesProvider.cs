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

namespace ClientCommon.Providers
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
        private async Task<List<BaseInstalledFixEntity>> CreateCacheAsync()
        {
            _logger.Info("Requesting installed fixes");

            if (!File.Exists(Consts.InstalledFile))
            {
                _cache = ConvertXmlToJson();
            }
            else
            {
                var text = await File.ReadAllTextAsync(Consts.InstalledFile).ConfigureAwait(false);

                text.ThrowIfNull();

                var installedFixes = JsonSerializer.Deserialize(text, InstalledFixesListContext.Default.ListBaseInstalledFixEntity);

                installedFixes.ThrowIfNull();

                var needToSave = FixRegValueType(installedFixes);
                needToSave = FixWrongGuids(installedFixes);

                _cache = installedFixes;

                if (needToSave)
                {
                    SaveInstalledFixes();
                }
            }

            return _cache;
        }

        /// <summary>
        /// Add value type to installed reg fix
        /// </summary>
        [Obsolete("Remove in version 1.0")]
        private bool FixRegValueType(List<BaseInstalledFixEntity> installedFixes)
        {
            var needToSave = false;

            foreach (var fix in installedFixes)
            {
                if (fix is RegistryInstalledFixEntity regFix &&
                    regFix.ValueType is null)
                {
                    if (regFix.Guid == Guid.Parse("6f768f0a-7233-4f64-8cb2-27f6b1edd7c4"))
                    {
                        regFix.ValueType = RegistryValueTypeEnum.Dword;
                    }
                    else
                    {
                        regFix.ValueType = RegistryValueTypeEnum.String;
                    }

                    needToSave = true;
                }
            }

            return needToSave;
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

        /// <summary>
        /// Convert old installed.xml file to a newer installed.json
        /// </summary>
        [Obsolete("Remove in version 1.0")]
        private List<BaseInstalledFixEntity> ConvertXmlToJson()
        {
#pragma warning disable CS8602
            if (!File.Exists("installed.xml"))
            {
                return [];
            }

            var xdoc = XDocument.Load("installed.xml");

            List<BaseInstalledFixEntity> result = [];

            var fileFixes = xdoc.Descendants("FileInstalledFix");

            foreach (var fix in fileFixes)
            {
                var gameId = fix.Element("GameId").Value;
                var guid = fix.Element("Guid").Value;
                var version = fix.Element("Version").Value;
                var backupFolder = fix.Element("BackupFolder")?.Value;

                var filesList = fix.Elements("FilesList").Descendants().Select(static x => x.Value);

                result.Add(new FileInstalledFixEntity()
                {
                    GameId = int.Parse(gameId),
                    Guid = new Guid(guid),
                    Version = int.Parse(version),
                    BackupFolder = backupFolder,
                    FilesList = [.. filesList],
                    InstalledSharedFix = null
                });
            }

            var hostsFixes = xdoc.Descendants("HostsInstalledFix");

            foreach (var fix in hostsFixes)
            {
                var gameId = fix.Element("GameId").Value;
                var guid = fix.Element("Guid").Value;
                var version = fix.Element("Version").Value;

                var entriesList = fix.Elements("Entries").Descendants().Select(static x => x.Value);

                result.Add(new HostsInstalledFixEntity()
                {
                    GameId = int.Parse(gameId),
                    Guid = new Guid(guid),
                    Version = int.Parse(version),
                    Entries = [.. entriesList]
                });
            }

            var registryFixes = xdoc.Descendants("RegistryInstalledFix");

            foreach (var fix in registryFixes)
            {
                var gameId = fix.Element("GameId").Value;
                var guid = fix.Element("Guid").Value;
                var version = fix.Element("Version").Value;
                var key = fix.Element("Key").Value;
                var value = fix.Element("ValueName").Value;
                var original = fix.Element("OriginalValue")?.Value;

                result.Add(new RegistryInstalledFixEntity()
                {
                    GameId = int.Parse(gameId),
                    Guid = new Guid(guid),
                    Version = int.Parse(version),
                    Key = key,
                    ValueName = value,
                    OriginalValue = original,
                    ValueType = RegistryValueTypeEnum.Dword
                });
            }

            SaveInstalledFixes();

            return result;
#pragma warning restore CS8602
        }
    }
}
