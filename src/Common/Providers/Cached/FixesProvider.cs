using Common.Config;
using Common.Entities.Fixes;
using Common.Entities.Fixes.FileFix;
using Common.Helpers;
using System.Collections.Immutable;
using System.Net.Http.Json;
using System.Text.Json;

namespace Common.Providers.Cached
{
    public sealed class FixesProvider : CachedProviderBase<FixesList>
    {
        private string? _fixesCachedString;
        private ImmutableList<FileFixEntity> _sharedFixes;
        private readonly ConfigEntity _config;
        private readonly HttpClient _httpClient;

        public FixesProvider(
            ConfigProvider config,
            HttpClient httpClient,
            Logger logger
            ) : base(logger)
        {
            _config = config.Config;
            _httpClient = httpClient;
        }

        /// <summary>
        /// Check if fix with the same GUID already exists in the database
        /// </summary>
        /// <returns>true if fix exists</returns>
        public async Task<bool> CheckIfFixExistsInTheDatabase(Guid guid)
        {
            _logger.Info("Requesting online fixes");

            var str = await _httpClient.GetStringAsync($"{ApiProperties.ApiUrl}/fixes/{guid}").ConfigureAwait(false);
            var result = bool.TryParse(str, out var doesExist);

            return result ? doesExist : true;
        }

        public ImmutableList<FileFixEntity> GetSharedFixes() => _sharedFixes;

        public async Task<Result> ChangeFixDisabledStateAsync(Guid fixGuid, bool isDeleted)
        {
            Tuple<Guid, bool, string> message = new(fixGuid, isDeleted, _config.ApiPassword);

            var result = await _httpClient.PutAsJsonAsync($"{ApiProperties.ApiUrl}/fixes/delete", message).ConfigureAwait(false);

            if (result.IsSuccessStatusCode)
            {
                return new(ResultEnum.Success, $"Successfully {(isDeleted ? "deleted" : "restored")} fix");
            }
            else
            {
                return new(ResultEnum.Error, $"Error while {(isDeleted ? "deleting" : "restoring")} fix");
            }
        }

        /// <summary>
        /// Save list of fixes to XML
        /// </summary>
        /// <param name="fix"></param>
        public async Task<Result> AddFixToDbAsync(int gameId, string gameName, BaseFixEntity fix)
        {
            var fileFixResult = PrepareFixes(fix);

            if (fileFixResult != ResultEnum.Success)
            {
                return fileFixResult;
            }

            var jsonStr = JsonSerializer.Serialize(
                    fix,
                    FixesListContext.Default.BaseFixEntity
                    );

            Tuple<int, string, string, string> message = new(gameId, gameName, jsonStr, _config.ApiPassword);

            var result = await _httpClient.PostAsJsonAsync($"{ApiProperties.ApiUrl}/fixes/add", message).ConfigureAwait(false);

            if (result.IsSuccessStatusCode)
            {
                return new(ResultEnum.Success, $"Successfully added fix");
            }
            else
            {
                return new(ResultEnum.Error, $"Error while adding fix");
            }
        }

        /// <summary>
        /// Get cached fixes list from online or local repo or create new cache if it wasn't created yet
        /// </summary>
        protected override async Task<ImmutableList<FixesList>> GetCachedListAsync()
        {
            _logger.Info("Requesting cached fixes list");

            await _locker.WaitAsync().ConfigureAwait(false);

            if (_fixesCachedString is null)
            {
                _locker.Release();

                return await CreateCacheAsync().ConfigureAwait(false);
            }

            _locker.Release();

            return [.. DeserializeCachedString(_fixesCachedString)];
        }

        private Result PrepareFixes(BaseFixEntity fix)
        {
            if (fix is FileFixEntity fileFix)
            {
                var result = PrepareFileFixes(fileFix);

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

            return new Result(ResultEnum.Success, string.Empty);
        }

        private Result PrepareFileFixes(FileFixEntity fileFix)
        {
            if (!string.IsNullOrEmpty(fileFix.Url))
            {
                if (!fileFix.Url.StartsWith("http"))
                {
                    fileFix.Url = Consts.FilesBucketUrl + fileFix.Url;
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
        /// Create new cache of fixes from online or local repository
        /// </summary>
        internal override async Task<ImmutableList<FixesList>> CreateCacheAsync()
        {
            _logger.Info("Creating fixes cache");

            try
            {
                _logger.Info("Downloading fixes xml from online repository");

                _fixesCachedString = await _httpClient.GetStringAsync($"{ApiProperties.ApiUrl}/fixes").ConfigureAwait(false);

                var fixes = DeserializeCachedString(_fixesCachedString);

                _sharedFixes = fixes.FirstOrDefault(static x => x.GameId == 0)?.Fixes.Select(static x => (FileFixEntity)x).ToImmutableList() ?? [];

                return fixes;
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message);
                throw;
            }
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
        /// Remove current cache, then create new one and return list of entities
        /// </summary>
        /// <returns>List of entities</returns>
        protected override Task<ImmutableList<FixesList>> GetNewListAsync()
        {
            _logger.Info($"Requesting new Fixes list");

            _cache = null;
            _fixesCachedString = null;

            return GetCachedListAsync();
        }
    }
}
