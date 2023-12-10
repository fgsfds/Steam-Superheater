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
    public static class InstalledFixesProvider
    {
        /// <summary>
        /// Remove current cache, then create new one and return installed fixes list
        /// </summary>
        /// <returns></returns>
        public static ImmutableList<BaseInstalledFixEntity> GetInstalledFixes()
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

            var fixesDatabase = JsonSerializer.Deserialize(text, InstalledFixesListContext.Default.ListBaseInstalledFixEntity);

            if (fixesDatabase is null)
            {
                ThrowHelper.NullReferenceException(nameof(fixesDatabase));
            }

            return [.. fixesDatabase];
        }

        /// <summary>
        /// Save list of installed fixes from combined entities list
        /// </summary>
        /// <param name="combinedEntitiesList">List of combined entities</param>
        /// <returns>result, error message</returns>
        public static Result SaveInstalledFixes(List<FixFirstCombinedEntity> combinedEntitiesList)
        {
            var installedFixes = CombinedEntitiesProvider.GetInstalledFixesFromCombined(combinedEntitiesList);

            var result = SaveInstalledFixes(installedFixes);

            return result;
        }

        /// <summary>
        /// Save installed fixes to XML
        /// </summary>
        /// <param name="fixesList">List of installed fix entities</param>
        /// <returns>result, error message</returns>
        public static Result SaveInstalledFixes(List<BaseInstalledFixEntity> fixesList)
        {
            Logger.Info("Saving installed fixes list");

            try
            {
                var json = JsonSerializer.Serialize(
                    fixesList,
                    InstalledFixesListContext.Default.ListBaseInstalledFixEntity
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
        /// Convert old installed.xml file to a newer installed.json
        /// </summary>
        [Obsolete("Remove in version 1.0")]
        private static ImmutableList<BaseInstalledFixEntity> ConvertXmlToJson()
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

            SaveInstalledFixes(result);

            return [.. result];
        }
    }
}
