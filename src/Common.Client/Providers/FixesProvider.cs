using Common.Client.API;
using Common.Entities.Fixes;
using Common.Entities.Fixes.FileFix;
using Common.Helpers;
using System.Collections.Immutable;

namespace Common.Client.Providers
{
    public sealed class FixesProvider
    {
        public ImmutableList<FileFixEntity> SharedFixes { get; private set; } = [];

        private readonly ApiInterface _apiInterface;
        private readonly Logger _logger;

        public FixesProvider(
            ApiInterface apiInterface,
            Logger logger
            )
        {
            _apiInterface = apiInterface;
            _logger = logger;
        }

        /// <summary>
        /// Get cached fixes list from online or local repo or create new cache if it wasn't created yet
        /// </summary>
        public async Task<Result<List<FixesList>>> GetFixesListAsync()
        {
            _logger.Info("Creating fixes cache");

            var result = await _apiInterface.GetFixesListAsync().ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                return new(result.ResultEnum, null, result.Message);
            }

            SharedFixes = result.ResultObject.FirstOrDefault(static x => x.GameId == 0)?.Fixes.Select(static x => (FileFixEntity)x).ToImmutableList() ?? [];

            return new(ResultEnum.Success, result.ResultObject, string.Empty);
        }

        /// <summary>
        /// Check if fix with the same GUID already exists in the database
        /// </summary>
        /// <returns>true if fix exists</returns>
        public async Task<bool> CheckIfFixExistsInTheDatabase(Guid guid)
        {
            _logger.Info("Requesting online fixes");

            var result = await _apiInterface.CheckIfFixEsistsAsync(guid).ConfigureAwait(false);

            return result.IsSuccess;
        }

        public async Task<Result> ChangeFixDisabledStateAsync(Guid fixGuid, bool isDeleted)
        {
            var result = await _apiInterface.ChangeFixStateAsync(fixGuid, isDeleted).ConfigureAwait(false);

            return result;
        }

        /// <summary>
        /// Upload fix to the database
        /// </summary>
        /// <param name="gameId">Game id</param>
        /// <param name="gameName">Game name</param>
        /// <param name="fix">Fix</param>
        public async Task<Result> AddFixToDbAsync(int gameId, string gameName, BaseFixEntity fix)
        {
            var fileFixResult = PrepareFixes(fix);

            if (fileFixResult != ResultEnum.Success)
            {
                return fileFixResult;
            }

            var result = await _apiInterface.AddFixToDbAsync(gameId, gameName, fix).ConfigureAwait(false);

            return result;
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
    }
}
