using Common.Config;
using Common.Entities;
using Common.Providers;
using System.Collections.Immutable;

namespace Common.Models
{
    public sealed class NewsModel
    {
        private readonly ConfigEntity _config;
        private readonly NewsProvider _newsProvider;

        public int UnreadNewsCount => News?.Where(x => x.IsNewer)?.Count() ?? 0;

        public bool HasUnreadNews => UnreadNewsCount > 0;

        public ImmutableList<NewsEntity> News;

        public NewsModel(
            ConfigProvider config,
            NewsProvider news)
        {
            _config = config?.Config ?? throw new NullReferenceException(nameof(config));
            _newsProvider = news ?? throw new NullReferenceException(nameof(news));
        }

        public async Task UpdateNewsListAsync()
        {
            News = await _newsProvider.GetNewsListAsync();

            UpdateReadStatusOfExistingNews();
        }

        public async Task MarkAllAsReadAsync()
        {
            UpdateConfigLastReadVersion();

            await UpdateNewsListAsync();
        }

        private void UpdateReadStatusOfExistingNews()
        {
            foreach (var item in News)
            {
                item.IsNewer = item.Version > _config.LastReadNewsVersion;
            }
        }

        private void UpdateConfigLastReadVersion()
        {
            var lastReadVersion = News.Max(x => x.Version);

            _config.LastReadNewsVersion = lastReadVersion;
        }
    }
}
