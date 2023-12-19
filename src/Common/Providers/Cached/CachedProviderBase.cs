using System.Collections.Immutable;

namespace Common.Providers
{
    public abstract class CachedProviderBase<T>
    {
        protected readonly SemaphoreSlim _locker = new(1);
        protected ImmutableList<T>? _cache;

        /// <summary>
        /// Get list of fix entities with installed fixes
        /// </summary>
        public async Task<ImmutableList<T>> GetListAsync(bool useCache) =>
            useCache
            ? await GetCachedListAsync()
            : await GetNewListAsync();

        internal abstract ImmutableList<T> CreateCache();

        /// <summary>
        /// Get cached fixes list from online or local repo or create new cache if it wasn't created yet
        /// </summary>
        protected virtual async Task<ImmutableList<T>> GetCachedListAsync()
        {
            Logger.Info($"Requesting cached {typeof(T)} list");

            await _locker.WaitAsync();

            var result = _cache ?? CreateCache();

            _locker.Release();

            return result;
        }

        /// <summary>
        /// Remove current cache, then create new one and return fixes list
        /// </summary>
        private Task<ImmutableList<T>> GetNewListAsync()
        {
            Logger.Info($"Requesting new {typeof(T)} list");

            _cache = null;

            return GetCachedListAsync();
        }
    }
}