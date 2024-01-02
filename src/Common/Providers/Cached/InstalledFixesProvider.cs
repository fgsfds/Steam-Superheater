using Common.Entities.Fixes;
using Common.Entities.Fixes.FileFix;
using Common.Entities.Fixes.HostsFix;
using Common.Entities.Fixes.RegistryFix;
using Common.Helpers;
using System.Collections.Immutable;
using System.Text.Json;
using System.Xml.Linq;

namespace Common.Providers
{
    public sealed class InstalledFixesProvider(FixesProvider fixesProvider) : CachedProviderBase<BaseInstalledFixEntity>
    {
        private readonly FixesProvider _fixesProvider = fixesProvider;

        /// <summary>
        /// Save installed fixes to XML
        /// </summary>
        /// <param name="fixesList">List of installed fix entities</param>
        /// <returns>Result struct</returns>
        public Result SaveInstalledFixes()
        {
            Logger.Info("Saving installed fixes list");

            try
            {
                _cache.ThrowIfNull();

                var json = JsonSerializer.Serialize(
                    _cache,
                    InstalledFixesListContext.Default.ImmutableListBaseInstalledFixEntity
                    );

                File.WriteAllText(Consts.InstalledFile, json);

                Logger.Info("Fixes list saved successfully");

                return new(ResultEnum.Success, string.Empty);
            }
            catch (Exception ex) when (ex is FileNotFoundException || ex is DirectoryNotFoundException)
            {
                Logger.Error(ex.Message);
                return new Result(ResultEnum.NotFound, ex.Message);
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
                return new Result(ResultEnum.Error, ex.Message);
            }
        }

        /// <summary>
        /// Add installed fix to cache
        /// </summary>
        /// <param name="installedFix">Installed fix entity</param>
        internal void AddToCache(BaseInstalledFixEntity installedFix)
        {
            _cache.ThrowIfNull();

            _cache = _cache.Add(installedFix);
        }

        /// <summary>
        /// Remove installed fix from cache
        /// </summary>
        /// <param name="gameId">Game id</param>
        /// <param name="fixGuid">Fix guid</param>
        internal void RemoveFromCache(int gameId, Guid fixGuid)
        {
            _cache.ThrowIfNull();

            var toRemove = _cache.First(x => x.GameId == gameId && x.Guid == fixGuid);
            _cache = _cache.Remove(toRemove);
        }

        /// <inheritdoc/>
        internal override async Task<ImmutableList<BaseInstalledFixEntity>> CreateCacheAsync()
        {
            Logger.Info("Requesting installed fixes");

            if (!File.Exists(Consts.InstalledFile))
            {
                _cache = ConvertXmlToJson();
            }
            else
            {
                var text = await File.ReadAllTextAsync(Consts.InstalledFile);

                text.ThrowIfNull();

                var installedFixes = JsonSerializer.Deserialize(text, InstalledFixesListContext.Default.ImmutableListBaseInstalledFixEntity);

                installedFixes.ThrowIfNull();

                _cache = [.. installedFixes];
            }

            return _cache;
        }

        /// <summary>
        /// Convert old installed.xml file to a newer installed.json
        /// </summary>
        [Obsolete("Remove in version 1.0")]
        private ImmutableList<BaseInstalledFixEntity> ConvertXmlToJson()
        {
#pragma warning disable CS8602
            if (!File.Exists("installed.xml"))
            {
                return [];
            }

            XDocument xdoc = XDocument.Load("installed.xml");

            List<BaseInstalledFixEntity> result = [];

            var fileFixes = xdoc.Descendants("FileInstalledFix");

            foreach (var fix in fileFixes)
            {
                var gameid = fix.Element("GameId").Value;
                var guid = fix.Element("Guid").Value;
                var version = fix.Element("Version").Value;
                var backupFolder = fix.Element("BackupFolder")?.Value;

                var filesList = fix.Elements("FilesList").Descendants().Select(static x => x.Value);

                result.Add(new FileInstalledFixEntity()
                {
                    GameId = int.Parse(gameid),
                    Guid = new Guid(guid),
                    Version = int.Parse(version),
                    BackupFolder = backupFolder,
                    FilesList = [.. filesList]
                });
            }

            var hostsFixes = xdoc.Descendants("HostsInstalledFix");

            foreach (var fix in hostsFixes)
            {
                var gameid = fix.Element("GameId").Value;
                var guid = fix.Element("Guid").Value;
                var version = fix.Element("Version").Value;

                var entriesList = fix.Elements("Entries").Descendants().Select(static x => x.Value);

                result.Add(new HostsInstalledFixEntity()
                {
                    GameId = int.Parse(gameid),
                    Guid = new Guid(guid),
                    Version = int.Parse(version),
                    Entries = [.. entriesList]
                });
            }

            var registryFixes = xdoc.Descendants("RegistryInstalledFix");

            foreach (var fix in registryFixes)
            {
                var gameid = fix.Element("GameId").Value;
                var guid = fix.Element("Guid").Value;
                var version = fix.Element("Version").Value;
                var key = fix.Element("Key").Value;
                var value = fix.Element("ValueName").Value;
                var original = fix.Element("OriginalValue")?.Value;

                result.Add(new RegistryInstalledFixEntity()
                {
                    GameId = int.Parse(gameid),
                    Guid = new Guid(guid),
                    Version = int.Parse(version),
                    Key = key,
                    ValueName = value,
                    OriginalValue = original
                });
            }

            SaveInstalledFixes();

            return [.. result];
#pragma warning restore CS8602
        }
    }
}
