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
                News = await _newsProvider.GetNewsListAsync();
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

            UpdateReadStatusOfExistingNews();

            return new(ResultEnum.Success, string.Empty);
        }

        /// <summary>
        /// Mark all news as read
        /// </summary>
        /// <returns></returns>
        public async Task<Result> MarkAllAsReadAsync()
        {
            var result = await UpdateNewsListAsync();

            if (result.IsSuccess)
            {
                UpdateConfigLastReadVersion();
                UpdateReadStatusOfExistingNews();
            }

            return result;
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
