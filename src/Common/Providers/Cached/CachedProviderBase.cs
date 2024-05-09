using System.Collections.Immutable;

namespace Common.Providers.Cached
{
    public abstract class CachedProviderBase<T>
    {
        protected readonly Logger _logger;
        protected readonly SemaphoreSlim _locker = new(1);
        protected ImmutableList<T>? _cache;

        protected CachedProviderBase(Logger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Get list of entities
        /// </summary>
        /// <param name="useCache">Get cacked list</param>
        /// <returns>List of entities</returns>
        public async Task<ImmutableList<T>> GetListAsync(bool useCache) =>
            useCache
            ? await GetCachedListAsync().ConfigureAwait(false)
            : await GetNewListAsync().ConfigureAwait(false);

        internal abstract Task<ImmutableList<T>> CreateCacheAsync();

        /// <summary>
        /// Get cached list of entities from online or local repo or create new cache if it wasn't created yet
        /// </summary>
        /// <returns>List of entities</returns>
        protected virtual async Task<ImmutableList<T>> GetCachedListAsync()
        {
            _logger.Info($"Requesting cached {typeof(T)} list");

            await _locker.WaitAsync().ConfigureAwait(false);

            var result = _cache ?? await CreateCacheAsync().ConfigureAwait(false);

            _locker.Release();

            return result;
        }

        /// <summary>
        /// Remove current cache, then create new one and return list of entities
        /// </summary>
        /// <returns>List of entities</returns>
        protected virtual Task<ImmutableList<T>> GetNewListAsync()
        {
            _logger.Info($"Requesting new {typeof(T)} list");

            _cache = null;

            return GetCachedListAsync();
        }
    }
}