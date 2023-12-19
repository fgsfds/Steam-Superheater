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
    public class InstalledFixesProvider
    {
        private ImmutableList<BaseInstalledFixEntity>? _cache;
        private readonly SemaphoreSlim _locker = new(1);

        /// <summary>
        /// Get list of fix entities with installed fixes
        /// </summary>
        public async Task<ImmutableList<BaseInstalledFixEntity>> GetListAsync(bool useCache) =>
            useCache
            ? await GetCachedListAsync()
            : await GetNewListAsync();

        public void AddToCache(BaseInstalledFixEntity fix)
        {
            _cache = _cache?.Add(fix) ?? ThrowHelper.NullReferenceException<ImmutableList<BaseInstalledFixEntity>>(nameof(_cache));
        }

        public void RemoveFromCache(BaseInstalledFixEntity fix)
        {
            _cache = _cache?.Remove(fix) ?? ThrowHelper.NullReferenceException<ImmutableList<BaseInstalledFixEntity>>(nameof(_cache));
        }

        /// <summary>
        /// Save installed fixes to XML
        /// </summary>
        /// <param name="fixesList">List of installed fix entities</param>
        /// <returns>result, error message</returns>
        public Result SaveInstalledFixes()
        {
            Logger.Info("Saving installed fixes list");

            try
            {
                var json = JsonSerializer.Serialize(
                    _cache,
                    InstalledFixesListContext.Default.ImmutableListBaseInstalledFixEntity
                    );

                File.WriteAllText(Consts.InstalledFile, json);

                return new(ResultEnum.Ok, string.Empty);
            }
            catch (Exception ex) when (ex is FileNotFoundException || ex is DirectoryNotFoundException)
            {
                Logger.Error(ex.Message);
                return new Result(ResultEnum.NotFound, ex.Message);
            }
        }

        /// <summary>
        /// Get cached fixes list from online or local repo or create new cache if it wasn't created yet
        /// </summary>
        private async Task<ImmutableList<BaseInstalledFixEntity>> GetCachedListAsync()
        {
            Logger.Info("Requesting cached games list");

            await _locker.WaitAsync();

            var result = _cache ?? CreateCache();

            _locker.Release();

            return result;
        }

        /// <summary>
        /// Remove current cache, then create new one and return fixes list
        /// </summary>
        private Task<ImmutableList<BaseInstalledFixEntity>> GetNewListAsync()
        {
            Logger.Info("Requesting new fixes list");

            _cache = null;

            return GetCachedListAsync();
        }

        /// <summary>
        /// Remove current cache, then create new one and return installed fixes list
        /// </summary>
        /// <returns></returns>
        private ImmutableList<BaseInstalledFixEntity> CreateCache()
        {
            Logger.Info("Requesting installed fixes");

            if (!File.Exists(Consts.InstalledFile))
            {
                return ConvertXmlToJson();
            }

            var text = File.ReadAllText(Consts.InstalledFile);

            if (text is null)
            {
                ThrowHelper.NullReferenceException(nameof(text));
            }

            var fixesDatabase = JsonSerializer.Deserialize(text, InstalledFixesListContext.Default.ImmutableListBaseInstalledFixEntity);

            if (fixesDatabase is null)
            {
                ThrowHelper.NullReferenceException(nameof(fixesDatabase));
            }

            _cache = [.. fixesDatabase];

            return _cache;
        }

        /// <summary>
        /// Convert old installed.xml file to a newer installed.json
        /// </summary>
        [Obsolete("Remove in version 1.0")]
        private ImmutableList<BaseInstalledFixEntity> ConvertXmlToJson()
        {
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
        }
    }
}
