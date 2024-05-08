using Common.Config;
using Common.Entities;
using Common.Helpers;
using System.Collections.Immutable;
using Common.Providers;

namespace Common.Models
{
    public sealed class NewsModel(
        ConfigProvider config,
        NewsProvider newsProvider
        )
    {
        private readonly ConfigEntity _config = config.Config;
        private readonly NewsProvider _newsProvider = newsProvider;

        public int UnreadNewsCount => News.Count(static x => x.IsNewer);

        public bool HasUnreadNews => UnreadNewsCount > 0;

        public ImmutableList<NewsEntity> News = [];

        /// <summary>
        /// Get list of news from online or local repo
        /// </summary>
        public async Task<Result> UpdateNewsListAsync()
        {
            try
            {
                News = await _newsProvider.GetNewsListAsync().ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
            {
                Logger.Error(ex.Message);
                return new(ResultEnum.NotFound, "File not found: " + ex.Message);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                Logger.Error(ex.Message);
                return new(ResultEnum.ConnectionError, "Can't connect to GitHub repository");
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
                return new(ResultEnum.ConnectionError, ex.Message);
            }

            UpdateReadStatusOfExistingNews();

            return new(ResultEnum.Success, string.Empty);
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
            var result1 = _newsProvider.ChangeNewsContent(date, content);

            if (result1 != ResultEnum.Success)
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
            var result1 = _newsProvider.AddNews(content);

            if (result1 != ResultEnum.Success)
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
        private void UpdateConfigLastReadVersion()
        {
            var lastReadDate = News.Max(static x => x.Date);

            _config.LastReadNewsDate = lastReadDate;
        }
    }
}
