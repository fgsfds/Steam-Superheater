using Common.Config;
using Common.Entities;
using Common.Helpers;
using System.Collections.Immutable;
using System.Net.Http.Json;
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
        public async Task<Result> ChangeNewsContentAsync(DateTime date, string content)
        {
            Tuple<DateTime, string, string> message = new(date, content, _config.ApiPassword);

            var result = await _httpClient.PutAsJsonAsync($"{ApiProperties.ApiUrl}/news/change", message).ConfigureAwait(false);

            if (result.IsSuccessStatusCode)
            {
                return new(ResultEnum.Success, string.Empty);
            }
            else
            {
                return new(ResultEnum.Error, "Error while changing news");
            }
        }

        /// <summary>
        /// Add news
        /// </summary>
        /// <param name="content">News content</param>
        public async Task<Result> AddNewsAsync(string content)
        {
            Tuple<DateTime, string, string> message = new(DateTime.UtcNow, content, _config.ApiPassword);

            var result = await _httpClient.PostAsJsonAsync($"{ApiProperties.ApiUrl}/news/add", message).ConfigureAwait(false);

            if (result.IsSuccessStatusCode)
            {
                return new(ResultEnum.Success, string.Empty);
            }
            else
            {
                return new(ResultEnum.Error, "Error while adding news");
            }
        }

        /// <summary>
        /// Create news list from json
        /// </summary>
        private async Task CreateNewsListAsync()
        {
            _logger.Info("Creating news list");

            var newsJson = await _httpClient.GetStringAsync($"{ApiProperties.ApiUrl}/news").ConfigureAwait(false);

            if (newsJson is null)
            {
                _logger.Error("Error while getting news...");
                ThrowHelper.Exception("Error while getting news");
            }

            var list = JsonSerializer.Deserialize(
                newsJson,
                NewsEntityContext.Default.ListNewsEntity
                );

            if (list is null)
            {
                _logger.Error("Error while deserializing news...");
                ThrowHelper.Exception("Error while deserializing news");
            }

            _news = [.. list];
        }
    }
}
