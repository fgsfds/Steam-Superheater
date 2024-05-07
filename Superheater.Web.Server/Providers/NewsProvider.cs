﻿using Common;
using Common.Entities;
using Superheater.Web.Server.Helpers;
using System.Collections.Immutable;
using System.Text.Json;

namespace Superheater.Web.Server.Providers
{
    public sealed class NewsProvider
    {
        private readonly HttpClientInstance _httpClient;
        private readonly ILogger<NewsProvider> _logger;
        private readonly string _jsonUrl = $"{Properties.FilesBucketUrl}news.json";

        private DateTime? _newsListLastModified;
        private ImmutableList<NewsEntity> _newsList;

        public ImmutableList<NewsEntity> NewsList => _newsList;


        public NewsProvider(ILogger<NewsProvider> logger, HttpClientInstance httpClient)
        {
            _logger = logger;
            _httpClient = httpClient;
        }


        public async Task CreateNewsList()
        {
            using var response = await _httpClient.GetAsync(new(_jsonUrl), HttpCompletionOption.ResponseHeadersRead);

            if (response.Content.Headers.LastModified is null)
            {
                _logger.LogError("Can't get last modified date");
                return;
            }

            if (_newsListLastModified is not null &&
                response.Content.Headers.LastModified <= _newsListLastModified)
            {
                return;
            }

            var json = await response.Content.ReadAsStringAsync();

            var newsList = JsonSerializer.Deserialize(json, NewsEntityContext.Default.ListNewsEntity);

            Interlocked.Exchange(ref _newsList, [.. newsList]);

            if (response.Content.Headers.LastModified is not null)
            {
                _newsListLastModified = response.Content.Headers.LastModified.Value.UtcDateTime;
            }
        }
    }
}