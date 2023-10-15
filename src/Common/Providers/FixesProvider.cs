using Common.Config;
using Common.Entities;
using Common.Helpers;
using System.Collections.Immutable;
using System.Reflection.Emit;
using System.Xml.Serialization;

namespace Common.Providers
{
    public sealed class FixesProvider
    {
        private bool _isCacheUpdating;
        private string? _fixesCachedString;
        private readonly ConfigEntity _config;

        public FixesProvider(ConfigProvider config)
        {
            _config = config.Config;
        }

        /// <summary>
        /// Get cached fixes list from online or local repo or create new cache if it wasn't created yet
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NullReferenceException"></exception>
        public async Task<ImmutableList<FixesList>> GetCachedListAsync()
        {
            while (_isCacheUpdating)
            {
                await Task.Delay(100);
            }

            if (_fixesCachedString is null)
            {
                await CreateFixesCacheAsync();
            }

            if (_fixesCachedString is null)
            {
                throw new Exception("Can't create fixes cache");
            }

            using (StringReader fs = new(_fixesCachedString))
            {
                return DeserializeCachedString(fs.ReadToEnd()).ToImmutableList();
            }
        }

        /// <summary>
        /// Remove current cache, then create new one and return fixes list
        /// </summary>
        /// <returns></returns>
        public async Task<ImmutableList<FixesList>> GetNewListAsync()
        {
            _fixesCachedString = null;

            return await GetCachedListAsync();
        }

        /// <summary>
        /// Get cached fixes list from online repo or create new cache if it wasn't created yet
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NullReferenceException"></exception>
        public async Task<ImmutableList<FixesList>> GetOnlineFixesListAsync()
        {
            if (_config.UseLocalRepo)
            {
                var xmlString = await DownloadFixesXMLAsync();

                return DeserializeCachedString(xmlString).ToImmutableList();
            }
            else
            {
                return await GetCachedListAsync();
            }
        }

        /// <summary>
        /// Save list of fixes to XML
        /// </summary>
        /// <param name="fixesList"></param>
        /// <returns></returns>
        public Tuple<bool, string> SaveFixes(List<FixesList> fixesList)
        {
            foreach (var fixes in fixesList)
            {
                foreach (var fix in fixes.Fixes)
                {
                    if (string.IsNullOrEmpty(fix.Url))
                    {
                        fix.Url = "empty.zip";
                    }

                    if (!fix.Url.StartsWith("http"))
                    {
                        fix.Url = Consts.MainFixesRepo + "/raw/master/fixes/" + fix.Url;
                    }
                }
            }

            XmlSerializer xmlSerializer = new(typeof(List<FixesList>));

            if (!Directory.Exists(CommonProperties.LocalRepoPath))
            {
                Directory.CreateDirectory(CommonProperties.LocalRepoPath);
            }

            try
            {
                using FileStream fs = new(Path.Combine(CommonProperties.LocalRepoPath, Consts.FixesFile), FileMode.Create);
                xmlSerializer.Serialize(fs, fixesList);
            }
            catch (Exception e) when (e is FileNotFoundException || e is DirectoryNotFoundException)
            {
                return new Tuple<bool, string>(false, e.Message);
            }

            return new Tuple<bool, string>(true, "XML saved successfully!");
        }

        /// <summary>
        /// Create new cache of fixes from online or local repository
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NullReferenceException"></exception>
        private async Task CreateFixesCacheAsync()
        {
            _isCacheUpdating = true;

            if (_config.UseLocalRepo)
            {
                var file = Path.Combine(CommonProperties.LocalRepoPath, Consts.FixesFile);

                if (!File.Exists(file))
                {
                    _isCacheUpdating = false;
                    throw new FileNotFoundException(file);
                }

                _fixesCachedString = File.ReadAllText(file);
            }
            else
            {
                _fixesCachedString = await DownloadFixesXMLAsync();
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
                using var stream = await client.GetStreamAsync(CommonProperties.CurrentFixesRepo + Consts.FixesFile);
                using var file = new StreamReader(stream);
                var fixesXml = await file.ReadToEndAsync();

                return fixesXml;
            }
        }
    }
}
