using ClientCommon.API;
using ClientCommon.Config;
using Common;
using Common.Entities;
using System.Collections.Immutable;

namespace ClientCommon.Models
{
    public sealed class NewsModel(
        ConfigProvider config,
        ApiInterface newsProvider,
        Logger logger
        )
    {
        private readonly ConfigEntity _config = config.Config;
        private readonly ApiInterface _newsProvider = newsProvider;
        private readonly Logger _logger = logger;

        public int UnreadNewsCount => News.Count(static x => x.IsNewer);

        public bool HasUnreadNews => UnreadNewsCount > 0;

        public ImmutableList<NewsEntity> News = [];

        /// <summary>
        /// Get list of news from online or local repo
        /// </summary>
        public async Task<Result> UpdateNewsListAsync()
        {
            var result = await _newsProvider.GetNewsListAsync().ConfigureAwait(false);

            if (result.IsSuccess)
            {
                News = [.. result.ResultObject];

                UpdateReadStatusOfExistingNews();
            }

            return new(result.ResultEnum, result.Message);
        }

        /// <summary>
        /// Mark all news as read
        /// </summary>
        public async Task<Result> MarkAllAsReadAsync()
        {
            UpdateConfigLastReadVersion();

            var result = await UpdateNewsListAsync().ConfigureAwait(false);

            return result;
        }

        /// <summary>
        /// Change content of the existing news
        /// </summary>
        /// <param name="date">Date of the news</param>
        /// <param name="content">Content</param>
        public async Task<Result> ChangeNewsContentAsync(DateTime date, string content)
        {
            var result1 = await _newsProvider.ChangeNewsAsync(date, content).ConfigureAwait(false);

            if (!result1.IsSuccess)
            {
                return result1;
            }

            var result2 = await UpdateNewsListAsync().ConfigureAwait(false);

            return result2;
        }

        /// <summary>
        /// Add news
        /// </summary>
        /// <param name="content">News content</param>
        public async Task<Result> AddNewsAsync(string content)
        {
            var result1 = await _newsProvider.AddNewsAsync(content).ConfigureAwait(false);

            if (!result1.IsSuccess)
            {
                return result1;
            }

            var result2 = await UpdateNewsListAsync().ConfigureAwait(false);

            return result2;
        }

        /// <summary>
        /// Set read status based on last read date from config
        /// </summary>
        private void UpdateReadStatusOfExistingNews()
        {
            foreach (var item in News)
            {
                item.IsNewer = item.Date > _config.LastReadNewsDate;
            }
        }

        /// <summary>
        /// Update last read date in config
        /// </summary>
        public void UpdateConfigLastReadVersion()
        {
            var lastReadDate = News.Max(static x => x.Date);

            _config.LastReadNewsDate = lastReadDate;
        }
    }
}
