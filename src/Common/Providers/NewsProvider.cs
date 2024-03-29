﻿using System.Collections.Immutable;
using System.Text.Json;
using Common.Config;
using Common.Entities;
using Common.Helpers;

namespace Common.Providers
{
    public sealed class NewsProvider(ConfigProvider config)
    {
        private readonly ConfigEntity _config = config.Config;

        /// <summary>
        /// Get list of news
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NullReferenceException"></exception>
        public async Task<ImmutableList<NewsEntity>> GetNewsListAsync()
        {
            Logger.Info("Requesting news");

            string? news;

            if (_config.UseLocalRepo)
            {
                var file = Path.Combine(_config.LocalRepoPath, Consts.NewsFile);

                if (!File.Exists(file))
                {
                    ThrowHelper.FileNotFoundException(file);
                }

                news = await File.ReadAllTextAsync(file);
            }
            else
            {
                news = await DownloadNewsXMLAsync();
            }

            var list = JsonSerializer.Deserialize(
                news,
                NewsEntityContext.Default.ListNewsEntity
                );

            if (list is null)
            {
                Logger.Error("Error while deserializing news...");

                return [];
            }

            return [.. list];
        }

        /// <summary>
        /// Get list of news from online repository
        /// </summary>
        /// <returns></returns>
        private async Task<string> DownloadNewsXMLAsync()
        {
            Logger.Info("Downloading news xml from online repository");

            try
            {
                using (HttpClient client = new())
                {
                    client.Timeout = TimeSpan.FromSeconds(10);
                    await using var stream = await client.GetStreamAsync(CommonProperties.CurrentFixesRepo + Consts.NewsFile);
                    using StreamReader file = new(stream);
                    var newsXml = await file.ReadToEndAsync();

                    return newsXml;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
                throw;
            }
        }
    }
}
