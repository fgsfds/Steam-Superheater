using Common.Entities.CombinedEntities;
using Common.Entities.Fixes;
using Common.Entities.Fixes.FileFix;
using Common.Helpers;
using System.Collections.Immutable;
using System.Text.Json;
using System.Xml.Serialization;

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
                return [];
            }

            var fixesDatabase = GetNewInstalledFixes();

            return [.. fixesDatabase];
        }

        /// <summary>
        /// Parse installed.xml and return a list of installed fixes
        /// </summary>
        /// <returns>List of installed fixes</returns>
        private static List<BaseInstalledFixEntity> GetNewInstalledFixes()
        {
            using (FileStream fs = new(Consts.InstalledFile, FileMode.Open))
            {
                var fixesDatabase = JsonSerializer.Deserialize(fs, InstalledFixesListContext.Default.ListBaseInstalledFixEntity);

                if (fixesDatabase is null)
                {
                    ThrowHelper.NullReferenceException(nameof(fixesDatabase));
                }

                return fixesDatabase;
            }
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
                using FileStream fs = new(Consts.InstalledFile, FileMode.Create);

                JsonSerializer.Serialize(
                    fs,
                    fixesList,
                    InstalledFixesListContext.Default.ListBaseInstalledFixEntity
                    );

                return new(ResultEnum.Ok, string.Empty);
            }
            catch (Exception ex) when (ex is FileNotFoundException || ex is DirectoryNotFoundException)
            {
                Logger.Error(ex.Message);
                return new Result(ResultEnum.NotFound, ex.Message);
            }
        }
    }
}
