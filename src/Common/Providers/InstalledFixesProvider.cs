using Common.CombinedEntities;
using Common.Entities.Fixes;
using Common.Entities.Fixes.FileFix;
using Common.Helpers;
using System.Collections.Immutable;
using System.Xml.Serialization;

namespace Common.Providers
{
    public static class InstalledFixesProvider
    {
        /// <summary>
        /// Remove current cache, then create new one and return installed fixes list
        /// </summary>
        /// <returns></returns>
        public static ImmutableList<IInstalledFixEntity> GetInstalledFixes()
        {
            if (!File.Exists(Consts.InstalledFile))
            {
                MakeEmptyFixesXml();
            }

            List<IInstalledFixEntity>? fixesDatabase;

            try
            {
                fixesDatabase = GetNewInstalledFixes();
            }
            catch
            {
                fixesDatabase = GetOldInstalledFixes();
            }

            return fixesDatabase.ToImmutableList();
        }

        private static List<IInstalledFixEntity> GetNewInstalledFixes()
        {
            XmlSerializer xmlSerializer = new(typeof(List<IInstalledFixEntity>));

            List<IInstalledFixEntity>? fixesDatabase;

            using (FileStream fs = new(Consts.InstalledFile, FileMode.Open))
            {
                fixesDatabase = xmlSerializer.Deserialize(fs) as List<IInstalledFixEntity>;
            }

            if (fixesDatabase is null)
            {
                throw new NullReferenceException(nameof(fixesDatabase));
            }

            return fixesDatabase;
        }

        [Obsolete("Remove in version 1.0")]
        private static List<IInstalledFixEntity> GetOldInstalledFixes()
        {
            XmlSerializer xmlSerializer = new(typeof(List<InstalledFixEntity_Obsolete>));

            List<InstalledFixEntity_Obsolete>? fixesDatabase;

            using (FileStream fs = new(Consts.InstalledFile, FileMode.Open))
            {
                fixesDatabase = xmlSerializer.Deserialize(fs) as List<InstalledFixEntity_Obsolete>;
            }

            if (fixesDatabase is null)
            {
                throw new NullReferenceException(nameof(fixesDatabase));
            }

            return fixesDatabase.ConvertAll(x => (IInstalledFixEntity)x);
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
        public static Result SaveInstalledFixes(List<IInstalledFixEntity> fixesList)
        {
            XmlSerializer xmlSerializer = new(typeof(List<FileInstalledFixEntity>));

            try
            {
                using FileStream fs = new(Consts.InstalledFile, FileMode.Create);
                xmlSerializer.Serialize(fs, fixesList);
            }
            catch (Exception e) when (e is FileNotFoundException || e is DirectoryNotFoundException)
            {
                return new Result(ResultEnum.NotFound, e.Message);
            }

            return new (ResultEnum.Ok, string.Empty);
        }

        /// <summary>
        /// Create empty installed fixes XML
        /// </summary>
        private static void MakeEmptyFixesXml() => SaveInstalledFixes(new List<IInstalledFixEntity>());
    }
}
