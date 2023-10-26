using Common.Config;
using Common.Entities;
using Common.Helpers;
using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Xml.Serialization;

namespace Common.Providers
{
    public sealed class FixesProvider
    {
        private string? _fixesCachedString;
        private readonly ConfigEntity _config;
        private readonly SemaphoreSlim _locker = new(1, 1);

        public FixesProvider(ConfigProvider config)
        {
            _config = config.Config;
        }

        /// <summary>
        /// Get cached fixes list from online or local repo or create new cache if it wasn't created yet
        /// </summary>
        public async Task<ImmutableList<FixesList>> GetCachedListAsync()
        {
            await _locker.WaitAsync();

            if (_fixesCachedString is null)
            {
                await CreateCacheAsync();
            }

            _locker.Release();

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
        public static async Task<Result> SaveFixesAsync(List<FixesList> fixesList)
        {
            using var client = new HttpClient();

            foreach (var fixes in fixesList)
            {
                foreach (var fix in fixes.Fixes)
                {
                    if (!string.IsNullOrEmpty(fix.Url))
                    {
                        if (!fix.Url.StartsWith("http"))
                        {
                            fix.Url = Consts.MainFixesRepo + "/raw/master/fixes/" + fix.Url;
                        }

                        if (fix.MD5 is null)
                        {
                            try
                            {
                                fix.MD5 = await GetMD5(client, fix);
                            }
                            catch (Exception e)
                            {
                                return new Result(ResultEnum.ConnectionError, e.Message);
                            }
                        }
                    }

                    if (fix.FilesToDelete is not null)
                    {
                        fix.FilesToDelete = fix.FilesToDelete.Where(x => !string.IsNullOrWhiteSpace(x)).ToList();

                        if (!fix.FilesToDelete.Any())
                        {
                            fix.FilesToDelete = null;
                        }
                    }

                    if (fix.FilesToBackup is not null)
                    {
                        fix.FilesToBackup = fix.FilesToBackup.Where(x => !string.IsNullOrWhiteSpace(x)).ToList();

                        if (!fix.FilesToBackup.Any())
                        {
                            fix.FilesToBackup = null;
                        }
                    }

                    if (fix.Variants is not null)
                    {
                        fix.Variants = fix.Variants.Where(x => !string.IsNullOrWhiteSpace(x)).ToList();

                        if (!fix.Variants.Any())
                        {
                            fix.Variants = null;
                        }
                    }

                    if (fix.Tags is not null)
                    {
                        fix.Tags = fix.Tags.Where(x => !string.IsNullOrWhiteSpace(x)).ToList();

                        if (!fix.Tags.Any())
                        {
                            fix.Tags = null;
                        }
                        else
                        {
                            var tags = fix.Tags
                                .Where(x => !string.IsNullOrWhiteSpace(x))
                                .OrderBy(x => x)
                                .ToList();

                            fix.Tags = tags;
                        }
                    }

                    if (fix.Dependencies is not null && !fix.Dependencies.Any())
                    {
                        fix.Dependencies = null;
                    }

                    if (fix.Url is not null && string.IsNullOrEmpty(fix.Url))
                    {
                        fix.Url = null;
                    }

                    if (fix.Description is not null && string.IsNullOrEmpty(fix.Description))
                    {
                        fix.Description = null;
                    }

                    if (fix.InstallFolder is not null && string.IsNullOrEmpty(fix.InstallFolder))
                    {
                        fix.InstallFolder = null;
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
                return new Result(ResultEnum.NotFound, e.Message);
            }

            return new(ResultEnum.Ok, "XML saved successfully!");
        }

        /// <summary>
        /// Get MD5 of the local or online file
        /// </summary>
        /// <param name="client">Http client</param>
        /// <param name="fix">Fix entity</param>
        /// <returns>MD5 of the fix file</returns>
        /// <exception cref="Exception">Http response error</exception>
        private static async Task<string> GetMD5(HttpClient client, FixEntity fix)
        {
            if (fix.Url is null) throw new NullReferenceException(nameof(fix.Url));

            if (fix.Url.StartsWith(Consts.MainFixesRepo + "/raw"))
            {
                var currentDir = Path.Combine(CommonProperties.LocalRepoPath, "fixes");
                var fileName = Path.GetFileName(fix.Url.ToString());
                var pathToFile = Path.Combine(currentDir, fileName);

                using (var md5 = MD5.Create())
                {
                    using (var stream = File.OpenRead(pathToFile))
                    {
                        return Convert.ToHexString(md5.ComputeHash(stream));
                    }
                }
            }
            else
            {
                using var response = await client.GetAsync(fix.Url, HttpCompletionOption.ResponseHeadersRead);

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Error while getting response for {fix.Url}: {response.StatusCode}");
                }
                else if (response.Content.Headers.ContentMD5 is not null)
                {
                    return BitConverter.ToString(response.Content.Headers.ContentMD5).Replace("-", string.Empty);
                }
                else
                {
                    //if can't get md5 from the response, download zip
                    var currentDir = Directory.GetCurrentDirectory();
                    var fileName = Path.GetFileName(fix.Url.ToString());
                    var pathToFile = Path.Combine(currentDir, fileName);

                    using (var file = new FileStream(pathToFile, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        using var source = await response.Content.ReadAsStreamAsync();

                        await source.CopyToAsync(file);
                    }

                    response.Dispose();

                    string hash;

                    using (var md5 = MD5.Create())
                    {
                        using var stream = File.OpenRead(pathToFile);

                        hash = Convert.ToHexString(md5.ComputeHash(stream));
                    }

                    File.Delete(pathToFile);
                    return hash;
                }
            }
        }

        /// <summary>
        /// Create new cache of fixes from online or local repository
        /// </summary>
        private async Task CreateCacheAsync()
        {
            if (_config.UseLocalRepo)
            {
                var file = Path.Combine(CommonProperties.LocalRepoPath, Consts.FixesFile);

                if (!File.Exists(file))
                {
                    throw new FileNotFoundException(file);
                }

                _fixesCachedString = File.ReadAllText(file);
            }
            else
            {
                _fixesCachedString = await DownloadFixesXMLAsync();
            }
        }

        /// <summary>
        /// Deserialize string
        /// </summary>
        /// <param name="fixes">String to deserialize</param>
        /// <returns>List of fixes</returns>
        private static List<FixesList> DeserializeCachedString(string fixes)
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
        private static async Task<string> DownloadFixesXMLAsync()
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
