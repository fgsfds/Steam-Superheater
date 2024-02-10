using Common.Config;
using Common.Entities.Fixes;
using Common.Entities.Fixes.FileFix;
using Common.Helpers;
using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text.Json;

namespace Common.Providers.Cached
{
    public sealed class FixesProvider(ConfigProvider config) : CachedProviderBase<FixesList>
    {
        private string? _fixesCachedString;
        private ImmutableList<FileFixEntity> _sharedFixes;
        private readonly ConfigEntity _config = config.Config;

        /// <summary>
        /// Get cached fixes list from online repo or create new cache if it wasn't created yet
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NullReferenceException"></exception>
        public async Task<ImmutableList<FixesList>> GetOnlineFixesListAsync()
        {
            Logger.Info("Requesting online fixes");

            if (_config.UseLocalRepo)
            {
                var xmlString = await DownloadFixesXMLAsync();

                return [.. DeserializeCachedString(xmlString)];
            }
            else
            {
                return await GetCachedListAsync();
            }
        }

        public ImmutableList<FileFixEntity> GetSharedFixes() => _sharedFixes;

        /// <summary>
        /// Save list of fixes to XML
        /// </summary>
        /// <param name="fixesList"></param>
        /// <returns></returns>
        public async Task<Result> SaveFixesAsync(List<FixesList> fixesList)
        {
            Logger.Info("Saving fixes list");

            var fileFixResult = await PrepareFixes(fixesList);

            if (fileFixResult != ResultEnum.Success)
            {
                return fileFixResult;
            }

            fixesList = [.. fixesList.OrderBy(static x => x.GameName)];

            try
            {
                if (!Directory.Exists(_config.LocalRepoPath))
                {
                    Directory.CreateDirectory(_config.LocalRepoPath);
                }

                await using FileStream fs = new(Path.Combine(_config.LocalRepoPath, Consts.FixesFile), FileMode.Create);

                JsonSerializer.Serialize(
                    fs,
                    fixesList,
                    FixesListContext.Default.ListFixesList
                    );
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
                return new(ResultEnum.Error, ex.Message);
            }

            Logger.Info("Fixes list saved successfully!");
            return new(ResultEnum.Success, "Fixes list saved successfully!");   
        }

        /// <summary>
        /// Get cached fixes list from online or local repo or create new cache if it wasn't created yet
        /// </summary>
        protected override async Task<ImmutableList<FixesList>> GetCachedListAsync()
        {
            Logger.Info("Requesting cached fixes list");

            await _locker.WaitAsync();

            if (_fixesCachedString is null)
            {
                _locker.Release();

                return await CreateCacheAsync();
            }

            _locker.Release();

            return [.. DeserializeCachedString(_fixesCachedString)];
        }

        private async Task<Result> PrepareFixes(List<FixesList> fixesList)
        {
            foreach (var fix in fixesList.SelectMany(static x => x.Fixes))
            {
                if (fix is FileFixEntity fileFix)
                {
                    var result = await PrepareFileFixes(fileFix);

                    if (!result.IsSuccess)
                    {
                        return result;
                    }
                }

                if (string.IsNullOrWhiteSpace(fix.Description))
                {
                    fix.Description = null;
                }
                if (string.IsNullOrWhiteSpace(fix.Notes))
                {
                    fix.Notes = null;
                }
                if (fix.Dependencies?.Count == 0)
                {
                    fix.Dependencies = null;
                }
                if (fix.Tags?.Any(static x => string.IsNullOrWhiteSpace(x)) ?? false)
                {
                    fix.Tags = null;
                }
            }

            return new Result(ResultEnum.Success, string.Empty);
        }

        private async Task<Result> PrepareFileFixes(FileFixEntity fileFix)
        {
            using HttpClient client = new();

            if (!string.IsNullOrEmpty(fileFix.Url))
            {
                if (!fileFix.Url.StartsWith("http"))
                {
                    fileFix.Url = Consts.MainFixesRepo + "/raw/master/fixes/" + fileFix.Url;
                }

                if (fileFix.MD5 is null)
                {
                    try
                    {
                        fileFix.MD5 = await GetMD5(client, fileFix);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex.Message);
                        return new Result(ResultEnum.ConnectionError, ex.Message);
                    }
                }

                if (fileFix.FileSize is null)
                {
                    fileFix.FileSize = await GetFileSize(client, fileFix);
                }
            }

            if (string.IsNullOrWhiteSpace(fileFix.RunAfterInstall))
            {
                fileFix.RunAfterInstall = null;
            }
            if (string.IsNullOrWhiteSpace(fileFix.InstallFolder))
            {
                fileFix.InstallFolder = null;
            }
            if (string.IsNullOrWhiteSpace(fileFix.ConfigFile))
            {
                fileFix.ConfigFile = null;
            }
            if (fileFix.FilesToBackup?.Any(static x => string.IsNullOrWhiteSpace(x)) ?? false)
            {
                fileFix.FilesToBackup = null;
            }
            if (fileFix.FilesToDelete?.Any(static x => string.IsNullOrWhiteSpace(x)) ?? false)
            {
                fileFix.FilesToDelete = null;
            }
            if (fileFix.FilesToPatch?.Any(static x => string.IsNullOrWhiteSpace(x)) ?? false)
            {
                fileFix.FilesToPatch = null;
            }
            if (string.IsNullOrWhiteSpace(fileFix.SharedFixInstallFolder))
            {
                fileFix.SharedFixInstallFolder = null;
            }

            return new Result(ResultEnum.Success, string.Empty);
        }

        /// <summary>
        /// Get MD5 of the local or online file
        /// </summary>
        /// <param name="client">Http client</param>
        /// <param name="fix">Fix entity</param>
        /// <returns>MD5 of the fix file</returns>
        /// <exception cref="Exception">Http response error</exception>
        private async Task<string> GetMD5(HttpClient client, FileFixEntity fix)
        {
            fix.Url.ThrowIfNull();

            if (fix.Url.StartsWith(Consts.MainFixesRepo + "/raw"))
            {
                var currentDir = Path.Combine(_config.LocalRepoPath, "fixes");
                var fileName = Path.GetRelativePath(Consts.MainFixesRepo + "/raw/master/fixes", fix.Url);
                //var fileName = Path.GetFileName(fix.Url);
                var pathToFile = Path.Combine(currentDir, fileName);

                using (var md5 = MD5.Create())
                {
                    await using (var stream = File.OpenRead(pathToFile))
                    {
                        return Convert.ToHexString(await md5.ComputeHashAsync(stream));
                    }
                }
            }
            else
            {
                using var response = await client.GetAsync(fix.Url, HttpCompletionOption.ResponseHeadersRead);

                if (!response.IsSuccessStatusCode)
                {
                    return ThrowHelper.Exception<string>($"Error while getting response for {fix.Url}: {response.StatusCode}");
                }
                else if (response.Content.Headers.ContentMD5 is not null)
                {
                    return BitConverter.ToString(response.Content.Headers.ContentMD5).Replace("-", string.Empty);
                }
                else
                {
                    //if can't get md5 from the response, download zip
                    var currentDir = Directory.GetCurrentDirectory();
                    var fileName = Path.GetFileName(fix.Url);
                    var pathToFile = Path.Combine(currentDir, fileName);

                    await using (FileStream file = new(pathToFile, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        await using var source = await response.Content.ReadAsStreamAsync();

                        await source.CopyToAsync(file);
                    }

                    response.Dispose();

                    string hash;

                    using (var md5 = MD5.Create())
                    {
                        await using var stream = File.OpenRead(pathToFile);

                        hash = Convert.ToHexString(await md5.ComputeHashAsync(stream));
                    }

                    File.Delete(pathToFile);
                    return hash;
                }
            }
        }

        /// <summary>
        /// Get the size the local or online file
        /// </summary>
        /// <param name="client">Http client</param>
        /// <param name="fix">Fix entity</param>
        /// <returns>Size of the file in bytes</returns>
        /// <exception cref="Exception">Http response error</exception>
        private async Task<long?> GetFileSize(HttpClient client, FileFixEntity fix)
        {
            fix.Url.ThrowIfNull();

            if (fix.Url.StartsWith(Consts.MainFixesRepo + "/raw"))
            {
                var currentDir = Path.Combine(_config.LocalRepoPath, "fixes");
                var fileName = Path.GetRelativePath(Consts.MainFixesRepo + "/raw/master/fixes", fix.Url);
                var pathToFile = Path.Combine(currentDir, fileName);

                FileInfo info = new(pathToFile);
                return info.Length;
            }
            else
            {
                using var response = await client.GetAsync(fix.Url, HttpCompletionOption.ResponseHeadersRead);

                if (!response.IsSuccessStatusCode)
                {
                    return ThrowHelper.Exception<long>($"Error while getting response for {fix.Url}: {response.StatusCode}");
                }
                else if (response.Content.Headers.ContentMD5 is not null)
                {
                    return response.Content.Headers.ContentLength;
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Create new cache of fixes from online or local repository
        /// </summary>
        internal override async Task<ImmutableList<FixesList>> CreateCacheAsync()
        {
            Logger.Info("Creating fixes cache");

            if (_config.UseLocalRepo)
            {
                var file = Path.Combine(_config.LocalRepoPath, Consts.FixesFile);

                if (!File.Exists(file))
                {
                    ThrowHelper.FileNotFoundException(file);
                }

                _fixesCachedString = File.ReadAllText(file);
            }
            else
            {
                _fixesCachedString = await DownloadFixesXMLAsync();
            }

            var fixes = DeserializeCachedString(_fixesCachedString);

            _sharedFixes = fixes.FirstOrDefault(static x => x.GameId == 0)?.Fixes.Select(static x => (FileFixEntity)x).ToImmutableList() ?? [];

            return fixes;
        }

        /// <summary>
        /// Deserialize string
        /// </summary>
        /// <param name="fixes">String to deserialize</param>
        /// <returns>List of fixes</returns>
        private static ImmutableList<FixesList> DeserializeCachedString(string fixes)
        {
            var fixesList = JsonSerializer.Deserialize(fixes, FixesListContext.Default.ListFixesList);

            fixesList.ThrowIfNull();

            return [.. fixesList];
        }

        /// <summary>
        /// Download fixes xml from online repository
        /// </summary>
        /// <returns></returns>
        private async Task<string> DownloadFixesXMLAsync()
        {
            Logger.Info("Downloading fixes xml from online repository");

            using (HttpClient client = new())
            {
                client.Timeout = TimeSpan.FromSeconds(10);
                await using var stream = await client.GetStreamAsync(CommonProperties.CurrentFixesRepo + Consts.FixesFile);
                using StreamReader file = new(stream);
                var fixesXml = file.ReadToEnd();

                return fixesXml;
            }
        }

        /// <summary>
        /// Remove current cache, then create new one and return list of entities
        /// </summary>
        /// <returns>List of entities</returns>
        protected override Task<ImmutableList<FixesList>> GetNewListAsync()
        {
            Logger.Info($"Requesting new Fixes list");

            _cache = null;
            _fixesCachedString = null;

            return GetCachedListAsync();
        }
    }
}
