using Common.Config;
using Common.Entities;
using Common.Helpers;
using Common.Providers;
using System.Collections.Immutable;

namespace Common.Models
{
    public sealed class NewsModel(
        ConfigProvider config,
        NewsProvider news
        )
    {
        private readonly ConfigEntity _config = config?.Config ?? ThrowHelper.ArgumentNullException<ConfigEntity>(nameof(config));
        private readonly NewsProvider _newsProvider = news ?? ThrowHelper.ArgumentNullException<NewsProvider>(nameof(news));

        public int UnreadNewsCount => News?.Where(x => x.IsNewer)?.Count() ?? 0;

        public bool HasUnreadNews => UnreadNewsCount > 0;

        public ImmutableList<NewsEntity> News;

        /// <summary>
        /// Get list of news from online or local repo
        /// </summary>
        public async Task<Result> UpdateNewsListAsync()
        {
            try
            {
                News = await _newsProvider.GetNewsListAsync();
            }
            catch (Exception ex) when (ex is FileNotFoundException || ex is DirectoryNotFoundException)
            {
                Logger.Error(ex.Message);
                return new(ResultEnum.NotFound, "File not found: " + ex.Message);
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
            {
                Logger.Error(ex.Message);
                return new(ResultEnum.ConnectionError, "Can't connect to GitHub repository");
            }

            UpdateReadStatusOfExistingNews();

            return new(ResultEnum.Ok, string.Empty);
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
            var lastReadDate = News.Max(x => x.Date);

            _config.LastReadNewsDate = lastReadDate;
        }
    }
}
