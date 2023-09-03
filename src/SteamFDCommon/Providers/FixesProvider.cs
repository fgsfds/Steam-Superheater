using SteamFDCommon.Config;
using SteamFDCommon.Entities;
using System.Xml.Serialization;

namespace SteamFDCommon.Providers
{
    public class FixesProvider
    {
        private bool _isCacheUpdating;

        private readonly ConfigEntity _config;

        //cached fixes from online or local repo
        private List<FixesList>? _fixesCache;
        //cached fixes from online repo only. used to check if the fix already exists in the repo before uploading.
        private List<FixesList>? _onlineFixes;

        public FixesProvider(ConfigProvider config)
        {
            _config = config.Config;
        }

        /// <summary>
        /// Get cached fixes list from online or local repo or create new cache if it wasn't created yet
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NullReferenceException"></exception>
        public async Task<List<FixesList>> GetCachedFixesListAsync()
        {
            while (_isCacheUpdating)
            {
                await Task.Delay(100);
            }

            if (_fixesCache is null)
            {
                await CreateFixesCacheAsync();
            }

            return _fixesCache;
        }

        /// <summary>
        /// Remove current cache, then create new one and return fixes list
        /// </summary>
        /// <returns></returns>
        public async Task<List<FixesList>> GetNewFixesListAsync()
        {
            _fixesCache = null;
            _onlineFixes = null;

            return await GetCachedFixesListAsync();
        }

        /// <summary>
        /// Get cached fixes list from online repo or create new cache if it wasn't created yet
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NullReferenceException"></exception>
        public async Task<List<FixesList>> GetOnlineFixesListAsync()
        {
            while (_isCacheUpdating)
            {
                await Task.Delay(100);
            }

            if (_onlineFixes is null)
            {
                await CreateFixesCacheAsync();
            }

            return _onlineFixes;
        }

        /// <summary>
        /// Create new cache of fixes from online or local repository
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NullReferenceException"></exception>
        private async Task CreateFixesCacheAsync()
        {
            _isCacheUpdating = true;

            string? fixes;
            string? onlineFixes;

            if (_config.UseLocalRepo)
            {
                var file = Path.Combine(Consts.LocalRepo, Consts.FixesFile);

                if (!File.Exists(file))
                {
                    _isCacheUpdating = false;
                    throw new FileNotFoundException(file);
                }

                fixes = File.ReadAllText(file);
                onlineFixes = await DownloadFixesXMLAsync();
            }
            else
            {
                try
                {
                    fixes = onlineFixes = await DownloadFixesXMLAsync();
                }
                catch
                {
                    throw;
                }
                finally
                {
                    _isCacheUpdating = false;
                }
            }

            using (StringReader fs = new(fixes))
            {
                _fixesCache = DeserializeCachedString(fs.ReadToEnd());
            }

            using (StringReader fs = new(onlineFixes))
            {
                _onlineFixes = DeserializeCachedString(fs.ReadToEnd());
            }

            _isCacheUpdating = false;
        }

        private List<FixesList> DeserializeCachedString(string fixes)
        {
            List<FixesList>? fixesDatabase;

            XmlSerializer xmlSerializer = new(typeof(List<FixesList>));

            using (TextReader reader = new StringReader(fixes))
            {
                fixesDatabase = xmlSerializer.Deserialize(reader) as List<FixesList>;
            }

            if (fixesDatabase is null)
            {
                throw new NullReferenceException(nameof(fixesDatabase));
            }

            return fixesDatabase;
        }

        /// <summary>
        /// Download fixes xml from online repository
        /// </summary>
        /// <returns></returns>
        private async Task<string> DownloadFixesXMLAsync()
        {
            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(10);
                using var stream = await client.GetStreamAsync(Consts.GitHubRepo + Consts.FixesFile);
                using var file = new StreamReader(stream);
                var fixesXml = await file.ReadToEndAsync();

                return fixesXml;
            }
        }

        /// <summary>
        /// Save list of fixes to XML
        /// </summary>
        /// <param name="fixesList"></param>
        /// <returns></returns>
        public static Tuple<bool, string> SaveFixes(List<FixesList> fixesList)
        {
            XmlSerializer xmlSerializer = new(typeof(List<FixesList>));

            try
            {
                using FileStream fs = new(Path.Combine(Consts.LocalRepo, Consts.FixesFile), FileMode.Create);
                xmlSerializer.Serialize(fs, fixesList);
            }
            catch (Exception e) when (e is FileNotFoundException || e is DirectoryNotFoundException)
            {
                return new Tuple<bool, string>(false, e.Message);
            }

            return new Tuple<bool, string>(true, "XML saved successfully!");
        }
    }
}
