using SteamFDCommon;
using SteamFDCommon.Config;
using SteamFDTCommon.Entities;
using System.IO;
using System.Net.Http;
using System.Xml.Serialization;

namespace SteamFDTCommon.Providers
{
    public class FixesProvider
    {
        private bool _isCacheUpdating;

        private readonly ConfigEntity _config;

        //private List<FixesList>? _fixesCache;

        private string? _fixesCache;

        public FixesProvider(ConfigProvider config)
        {
            _config = config.Config;
        }

        /// <summary>
        /// Get cached fixes list or create new cache if it wasn't created yet
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

            var fixesList = DeserializeCachedString();

            return fixesList;
        }

        /// <summary>
        /// Remove current cache, then create new one and return fixes list
        /// </summary>
        /// <returns></returns>
        public async Task<List<FixesList>> GetNewFixesListAsync()
        {
            _fixesCache = null;

            return await GetCachedFixesListAsync();
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

            if (_config.UseLocalRepo)
            {
                var file = Path.Combine(Consts.LocalRepo, Consts.FixesFile);

                if (!File.Exists(file))
                {
                    throw new FileNotFoundException(file);
                }

                fixes = File.ReadAllText(file);
            }
            else
            {
                fixes = await DownloadFixesXMLAsync();
            }

            using (StringReader fs = new(fixes))
            {
                _fixesCache = fs.ReadToEnd();
            }

            _isCacheUpdating = false;
        }

        public async Task<bool> UploadFixesToGitAsync()
        {
            //await GitUploader.UpdateFileInRepo(FixesXmlFile);

            //File.Delete(FixesXmlFile);

            return true;
        }

        private List<FixesList> DeserializeCachedString()
        {
            List<FixesList>? fixesDatabase;

            XmlSerializer xmlSerializer = new(typeof(List<FixesList>));

            using (TextReader reader = new StringReader(_fixesCache))
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
            try
            {
                using (var client = new HttpClient())
                {
                    using var stream = await client.GetStreamAsync(Consts.GitHubRepo + Consts.FixesFile);
                    using var file = new StreamReader(stream);
                    var fixesXml = await file.ReadToEndAsync();

                    return fixesXml;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Save list of fixes to XML
        /// </summary>
        /// <param name="fixesList"></param>
        /// <returns></returns>
        public static string SaveFixes(List<FixesList> fixesList)
        {
            XmlSerializer xmlSerializer = new(typeof(List<FixesList>));

            try
            {
                using FileStream fs = new(Path.Combine(Consts.LocalRepo, Consts.FixesFile), FileMode.Create);
                xmlSerializer.Serialize(fs, fixesList);
            }
            catch (Exception e) when (e is FileNotFoundException || e is DirectoryNotFoundException)
            {
                return e.Message;
            }

            return "XML saved successfully!";
        }
    }
}
