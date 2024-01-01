using Common.Entities.CombinedEntities;
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
    public sealed class InstalledFixesProvider : CachedProviderBase<BaseInstalledFixEntity>
    {
        /// <summary>
        /// Save list of installed fixes from combined entities list
        /// </summary>
        /// <param name="combinedEntitiesList">List of combined entities</param>
        /// <returns>Result struct</returns>
        public Result SaveInstalledFixes(ImmutableList<FixFirstCombinedEntity> combinedEntitiesList)
        {
            var installedFixes = CombinedEntitiesProvider.GetInstalledFixesFromCombined(combinedEntitiesList);

            var result = SaveInstalledFixes(installedFixes);

            return result;
        }

        /// <summary>
        /// Save installed fixes to XML
        /// </summary>
        /// <param name="fixesList">List of installed fix entities</param>
        /// <returns>Result struct</returns>
        public Result SaveInstalledFixes(ImmutableList<BaseInstalledFixEntity> fixesList)
        {
            Logger.Info("Saving installed fixes list");

            try
            {
                var json = JsonSerializer.Serialize(
                    fixesList,
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

        /// <inheritdoc/>
        internal override async Task<ImmutableList<BaseInstalledFixEntity>> CreateCache()
        {
            Logger.Info("Requesting installed fixes");

            if (!File.Exists(Consts.InstalledFile))
            {
                return ConvertXmlToJson();
            }

            var text = await File.ReadAllTextAsync(Consts.InstalledFile);

            text.ThrowIfNull();

            var fixesDatabase = JsonSerializer.Deserialize(text, InstalledFixesListContext.Default.ImmutableListBaseInstalledFixEntity);

            fixesDatabase.ThrowIfNull();

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

            SaveInstalledFixes([.. result]);

            return [.. result];
        }
    }
}
