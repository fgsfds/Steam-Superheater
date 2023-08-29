using SteamFDCommon.Config;
using SteamFDCommon.Entities;
using SteamFDCommon.Providers;

namespace SteamFDCommon.Models
{
    public class NewsModel
    {
        private readonly ConfigEntity _config;
        private readonly NewsProvider _newsProvider;

        private readonly List<NewsEntity> _news;
        private int _lastReadVersion;

        public int UnreadNewsCount => _news.Where(x => x.IsNewer).Count();

        public bool HasUnreadNews => UnreadNewsCount > 0;

        public List<NewsEntity> News => _news;

        public NewsModel(
            ConfigProvider config,
            NewsProvider news)
        {
            _config = config?.Config ?? throw new NullReferenceException(nameof(config));
            _newsProvider = news ?? throw new NullReferenceException(nameof(news));

            _lastReadVersion = _config.LastReadNewsVersion;
            _news = new();
        }

        public async Task UpdateNewsListAsync()
        {
            _news.Clear();

            var news = await _newsProvider.GetCachedNewsList();

            _news.AddRange(news);

            UpdateReadStatusOfExistingNews();
        }

        public void MarkAllAsRead()
        {
            UpdateConfigLastReadVersion();

            UpdateReadStatusOfExistingNews();
        }

        private void UpdateReadStatusOfExistingNews()
        {
            foreach (var item in _news)
            {
                item.IsNewer = item.Version > _lastReadVersion;
            }
        }

        private void UpdateConfigLastReadVersion()
        {
            _lastReadVersion = _news.Max(x => x.Version);

            _config.LastReadNewsVersion = _lastReadVersion;
        }
    }
}
