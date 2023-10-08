using Common.CombinedEntities;
using Common.Entities;
using Common.Helpers;
using System.Collections.Immutable;
using System.IO;
using System.Xml.Serialization;

namespace Common.Providers
{
    public sealed class InstalledFixesProvider
    {
        /// <summary>
        /// Remove current cache, then create new one and return installed fixes list
        /// </summary>
        /// <returns></returns>
        public ImmutableList<InstalledFixEntity> GetInstalledFixes()
        {
            if (!File.Exists(Consts.InstalledFile))
            {
                MakeEmptyFixesXml();
            }

            XmlSerializer xmlSerializer = new(typeof(List<InstalledFixEntity>));

            List<InstalledFixEntity>? fixesDatabase;

            using (FileStream fs = new(Consts.InstalledFile, FileMode.Open))
            {
                fixesDatabase = xmlSerializer.Deserialize(fs) as List<InstalledFixEntity>;
            }

            if (fixesDatabase is null)
            {
                throw new NullReferenceException(nameof(fixesDatabase));
            }

            return fixesDatabase.ToImmutableList();
        }

        /// <summary>
        /// Save list of installed fixes from combined entities list
        /// </summary>
        /// <param name="combinedEntitiesList">List of combined entities</param>
        /// <returns>result, error message</returns>
        public Tuple<bool, string> SaveInstalledFixes(List<FixFirstCombinedEntity> combinedEntitiesList)
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
        public Tuple<bool, string> SaveInstalledFixes(List<InstalledFixEntity> fixesList)
        {
            XmlSerializer xmlSerializer = new(typeof(List<InstalledFixEntity>));

            try
            {
                using FileStream fs = new(Consts.InstalledFile, FileMode.Create);
                xmlSerializer.Serialize(fs, fixesList);
            }
            catch (Exception e) when (e is FileNotFoundException || e is DirectoryNotFoundException)
            {
                return new Tuple<bool, string>(false, e.Message);
            }

            return new Tuple<bool, string>(true, string.Empty);
        }

        /// <summary>
        /// Create empty installed fixes XML
        /// </summary>
        private void MakeEmptyFixesXml() => SaveInstalledFixes(new List<InstalledFixEntity>());
    }
}
