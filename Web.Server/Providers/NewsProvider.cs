using Common;
using Common.Entities;
using Common.Helpers;
using System.Collections.Immutable;
using System.Text.Json;

namespace Superheater.Web.Server.Providers
{
    public sealed class NewsProvider
    {
        private readonly HttpClientInstance _httpClient;
        private readonly ILogger<NewsProvider> _logger;
        private readonly string _jsonUrl = $"{Consts.FilesBucketUrl}news.json";

        private DateTime? _newsListLastModified;
        private ImmutableList<NewsEntity> _newsList;

        public ImmutableList<NewsEntity> NewsList => _newsList;


        public NewsProvider(ILogger<NewsProvider> logger, HttpClientInstance httpClient)
        {
            _logger = logger;
            _httpClient = httpClient;
        }


        public async Task CreateNewsListAsync()
        {
            _logger.LogInformation("Looking for new news");

            using var response = await _httpClient.GetAsync(_jsonUrl, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);

            if (response.Content.Headers.LastModified is null)
            {
                _logger.LogError("Can't get news last modified date");
            }

            if (response.Content.Headers.LastModified <= _newsListLastModified)
            {
                _logger.LogInformation("No new news found");
                return;
            }

            var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            var newsList = JsonSerializer.Deserialize(json, NewsEntityContext.Default.ListNewsEntity);

            Interlocked.Exchange(ref _newsList, [.. newsList]);

            if (response.Content.Headers.LastModified is not null)
            {
                _newsListLastModified = response.Content.Headers.LastModified.Value.UtcDateTime;
            }

            _logger.LogInformation("Found new news");
        }
    }
}
