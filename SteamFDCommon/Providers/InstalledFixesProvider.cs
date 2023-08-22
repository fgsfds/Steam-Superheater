using SteamFDCommon;
using SteamFDTCommon.Entities;
using System.IO;
using System.Xml.Serialization;

namespace SteamFDTCommon.Providers
{
    public class InstalledFixesProvider
    {
        private bool _isCacheUpdating;

        private List<InstalledFixEntity>? _installedFixesCache;

        /// <summary>
        /// Get cached installed fixes list or create new cache if it wasn't created yet
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NullReferenceException"></exception>
        public List<InstalledFixEntity> GetCachedInstalledFixesList()
        {
            while (_isCacheUpdating)
            {
                Thread.Sleep(100);
            }

            if (_installedFixesCache is null)
            {
                CreateInstalledFixesCache();
            }

            return _installedFixesCache ?? throw new NullReferenceException(nameof(_installedFixesCache));
        }

        /// <summary>
        /// Remove current cache, then create new one and return installed fixes list
        /// </summary>
        /// <returns></returns>
        public List<InstalledFixEntity> GetNewInstalledFixesList()
        {
            _installedFixesCache = null;

            return GetCachedInstalledFixesList();
        }

        /// <summary>
        /// Create new cache of installed fixes
        /// </summary>
        /// <exception cref="NullReferenceException"></exception>
        private void CreateInstalledFixesCache()
        {
            _isCacheUpdating = true;

            if (!File.Exists(Consts.InstalledFile))
            {
                MakeEmptyFixesXml();
            }

            XmlSerializer xmlSerializer = new(typeof(List<InstalledFixEntity>));

            List<InstalledFixEntity>? fixesDatabase;

            using (FileStream fs = new(Consts.InstalledFile, FileMode.OpenOrCreate))
            {
                fixesDatabase = xmlSerializer.Deserialize(fs) as List<InstalledFixEntity>;
            }

            if (fixesDatabase is null)
            {
                throw new NullReferenceException(nameof(fixesDatabase));
            }

            _installedFixesCache = fixesDatabase;

            _isCacheUpdating = false;
        }

        /// <summary>
        /// Create empty installed fixes XML
        /// </summary>
        private void MakeEmptyFixesXml()
        {
            var fixesDatabase = new List<InstalledFixEntity>();

            SaveInstalledFixes(fixesDatabase);
        }

        /// <summary>
        /// Save installed fixes to XML
        /// </summary>
        /// <param name="fixesList"></param>
        /// <returns></returns>
        public static Tuple<bool, string> SaveInstalledFixes(List<InstalledFixEntity> fixesList)
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
    }
}
