using Common.Config;
using Common.Entities;
using Common.Helpers;
using System.Collections.Immutable;
using System.Text.Json;

namespace Common.Providers
{
    public sealed class NewsProvider(
        ConfigProvider config, 
        HttpClient httpClient,
        Logger logger
        )
    {
        private readonly ConfigEntity _config = config.Config;
        private readonly HttpClient _httpClient = httpClient;
        private readonly Logger _logger = logger;
        private List<NewsEntity> _news;

        /// <summary>
        /// Get list of news
        /// </summary>
        /// <exception cref="NullReferenceException"></exception>
        public async Task<ImmutableList<NewsEntity>> GetNewsListAsync()
        {
            _logger.Info("Requesting news");

            await CreateNewsListAsync().ConfigureAwait(false);

            return [.. _news];
        }

        /// <summary>
        /// Change content of the existing news
        /// </summary>
        /// <param name="date">Date of the news</param>
        /// <param name="content">Content</param>
        public Result ChangeNewsContent(DateTime date, string content)
        {
            var news = _news.FirstOrDefault(s => s.Date == date);

            if (news is null)
            {
                return new(ResultEnum.Error, "Can't find news with the selected date");
            }

            news.Content = content;
            var result = SaveNewsJson();

            return result;
        }

        /// <summary>
        /// Add news
        /// </summary>
        /// <param name="content">News content</param>
        public Result AddNews(string content)
        {
            _news.Add(new() { Date = DateTime.Now, Content = content });

            _news = [.. _news.OrderByDescending(x => x.Date)];

            var result = SaveNewsJson();

            return result;
        }

        /// <summary>
        /// Save news.json
        /// </summary>
        private Result SaveNewsJson()
        {
            try
            {
                using (FileStream fs = new(Path.Combine(_config.LocalRepoPath, Consts.NewsFile), FileMode.Create))
                {
                    JsonSerializer.Serialize(
                        fs,
                        _news,
                        NewsEntityContext.Default.ListNewsEntity
                       );
                }

                return new(ResultEnum.Success, string.Empty);
            }
            catch (Exception ex)
            {
                return new(ResultEnum.Error, ex.Message);
            }
        }

        /// <summary>
        /// Create news list from json
        /// </summary>
        private async Task CreateNewsListAsync()
        {
            _logger.Info("Creating news list");

            var news = await DownloadNewsXMLAsync().ConfigureAwait(false);

            var list = JsonSerializer.Deserialize(
                news,
                NewsEntityContext.Default.ListNewsEntity
                );

            if (list is null)
            {
                _logger.Error("Error while deserializing news...");

                ThrowHelper.Exception("Error while deserializing news");
            }

            _news = [.. list];
        }

        /// <summary>
        /// Get list of news from online repository
        /// </summary>
        private async Task<string> DownloadNewsXMLAsync()
        {
            _logger.Info("Downloading news xml from online repository");

            try
            {
                var newsJson = await _httpClient.GetStringAsync($"{CommonProperties.ApiUrl}/news").ConfigureAwait(false);

                return newsJson;
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message);
                throw;
            }
        }
    }
}
