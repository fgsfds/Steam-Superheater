using Common.Config;
using Common.Entities;
using Common.Helpers;
using System.Collections.Immutable;
using System.Xml.Serialization;

namespace Common.Providers
{
    public sealed class FixesProvider
    {
        private bool _isCacheUpdating;

        private readonly ConfigEntity _config;

        //cached fixes string from online or local repo
        private string? _fixesCachedString;
        //cached fixes from online repo only. used to check if the fix already exists in the repo before uploading.
        private ImmutableList<FixesList>? _onlineFixes;

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

            if (_fixesCachedString is null)
            {
                await CreateFixesCacheAsync();
            }

            using (StringReader fs = new(_fixesCachedString))
            {
                return DeserializeCachedString(fs.ReadToEnd());
            }
        }

        /// <summary>
        /// Remove current cache, then create new one and return fixes list
        /// </summary>
        /// <returns></returns>
        public async Task<List<FixesList>> GetNewFixesListAsync()
        {
            _fixesCachedString = null;
            _onlineFixes = null;

            return await GetCachedFixesListAsync();
        }

        /// <summary>
        /// Get cached fixes list from online repo or create new cache if it wasn't created yet
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NullReferenceException"></exception>
        public async Task<ImmutableList<FixesList>> GetOnlineFixesListAsync()
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

            string? onlineFixes = null;

            try
            {
                onlineFixes = await DownloadFixesXMLAsync();
            }
            catch
            {
                throw;
            }
            finally
            {
                _isCacheUpdating = false;
            }

            if (_config.UseLocalRepo)
            {
                var file = Path.Combine(CommonProperties.LocalRepo, Consts.FixesFile);

                if (!File.Exists(file))
                {
                    _isCacheUpdating = false;
                    throw new FileNotFoundException(file);
                }

                _fixesCachedString = File.ReadAllText(file);
            }
            else
            {
                _fixesCachedString = onlineFixes;
            }

            using (StringReader fs = new(onlineFixes))
            {
                var list = DeserializeCachedString(fs.ReadToEnd());

                _onlineFixes = ImmutableList.CreateRange(list);
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
                using var stream = await client.GetStreamAsync(Consts.MainFixesRepo + Consts.FixesFile);
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
            foreach (var fixes in fixesList)
            {
                foreach(var fix in fixes.Fixes)
                {
                    if (!fix.Url.StartsWith("http"))
                    {
                        fix.Url = Path.Combine(Consts.MainFixesRepo + "fixes/" + fix.Url);
                    }
                }
            }

            XmlSerializer xmlSerializer = new(typeof(List<FixesList>));

            if (!Directory.Exists(CommonProperties.LocalRepo))
            {
                Directory.CreateDirectory(CommonProperties.LocalRepo);
            }

            try
            {
                using FileStream fs = new(Path.Combine(CommonProperties.LocalRepo, Consts.FixesFile), FileMode.Create);
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
