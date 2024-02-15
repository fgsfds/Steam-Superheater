using System.Collections.Immutable;
using System.Collections.ObjectModel;

namespace Common.Providers.Cached
{
    public abstract class CachedProviderBase<K, V> where K : notnull
    {
        protected readonly SemaphoreSlim _locker = new(1);
        protected ImmutableDictionary<K, V>? _cache;

        /// <summary>
        /// Get list of entities
        /// </summary>
        /// <param name="useCache">Get cacked list</param>
        /// <returns>List of entities</returns>
        public async Task<ImmutableDictionary<K, V>> GetListAsync(bool useCache) =>
            useCache
            ? await GetCachedListAsync()
            : await GetNewListAsync();

        internal abstract Task<ImmutableDictionary<K, V>> CreateCacheAsync();

        /// <summary>
        /// Get cached list of entities from online or local repo or create new cache if it wasn't created yet
        /// </summary>
        /// <returns>List of entities</returns>
        protected virtual async Task<ImmutableDictionary<K, V>> GetCachedListAsync()
        {
            Logger.Info($"Requesting cached {typeof(K)} list");

            await _locker.WaitAsync();

            var result = _cache ?? await CreateCacheAsync();

            _locker.Release();

            return result;
        }

        /// <summary>
        /// Remove current cache, then create new one and return list of entities
        /// </summary>
        /// <returns>List of entities</returns>
        protected virtual Task<ImmutableDictionary<K, V>> GetNewListAsync()
        {
            Logger.Info($"Requesting new {typeof(K)} list");

            _cache = null;

            return GetCachedListAsync();
        }
    }
}