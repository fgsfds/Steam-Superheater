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

        public async Task<Tuple<bool, string>> UpdateNewsListAsync()
        {
            try
            {
                News = await _newsProvider.GetNewsListAsync();
            }
            catch (Exception ex) when (ex is FileNotFoundException || ex is DirectoryNotFoundException)
            {
                return new(false, "File not found: " + ex.Message);
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
            {
                return new(false, "Can't connect to GitHub repository");
            }

            UpdateReadStatusOfExistingNews();

            return new(true, string.Empty);
        }

        public async Task<Tuple<bool, string>> MarkAllAsReadAsync()
        {
            var result = await UpdateNewsListAsync();

            if (result.Item1)
            {
                UpdateConfigLastReadVersion();
                UpdateReadStatusOfExistingNews();
            }

            return result;
        }

        private void UpdateReadStatusOfExistingNews()
        {
            foreach (var item in News)
            {
                item.IsNewer = item.Date > _config.LastReadNewsDate;
            }
        }

        private void UpdateConfigLastReadVersion()
        {
            var lastReadDate = News.Max(x => x.Date);

            _config.LastReadNewsDate = lastReadDate;
        }
    }
}
