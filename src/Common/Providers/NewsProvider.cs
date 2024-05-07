using Common.Config;
using Common.Entities;
using Common.Helpers;
using System.Collections.Immutable;
using System.Text.Json;

namespace Common.Providers
{
    public sealed class NewsProvider(
        ConfigProvider config, 
        HttpClientInstance httpClient
        )
    {
        private readonly ConfigEntity _config = config.Config;
        private readonly HttpClientInstance _httpClient = httpClient;
        private List<NewsEntity> _news;

        /// <summary>
        /// Get list of news
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NullReferenceException"></exception>
        public async Task<ImmutableList<NewsEntity>> GetNewsListAsync()
        {
            Logger.Info("Requesting news");

            await CreateNewsListAsync();

            return [.. _news];
        }

        /// <summary>
        /// Change content of the existing news
        /// </summary>
        /// <param name="date">Date of the news</param>
        /// <param name="content">Content</param>
        public async Task<Result> ChangeNewsContentAsync(DateTime date, string content)
        {
            var news = _news.FirstOrDefault(s => s.Date == date);

            if (news is null)
            {
                return new(ResultEnum.Error, "Can't find news with the selected date");
            }

            news.Content = content;
            var result = await SaveNewsJsonAsync();

            return result;
        }

        /// <summary>
        /// Add news
        /// </summary>
        /// <param name="content">News content</param>
        public async Task<Result> AddNewsAsync(string content)
        {
            _news.Add(new() { Date = DateTime.Now, Content = content });

            _news = [.. _news.OrderByDescending(x => x.Date)];

            var result = await SaveNewsJsonAsync();

            return result;
        }

        /// <summary>
        /// Save news.json
        /// </summary>
        private async Task<Result> SaveNewsJsonAsync()
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
            Logger.Info("Creating news list");

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

                ThrowHelper.Exception("Error while deserializing news");
            }

            _news = [.. list];
        }

        /// <summary>
        /// Get list of news from online repository
        /// </summary>
        private async Task<string> DownloadNewsXMLAsync()
        {
            Logger.Info("Downloading news xml from online repository");

            try
            {
                var newsXml = await _httpClient.GetStringAsync(CommonProperties.CurrentFixesRepo + Consts.NewsFile);

                return newsXml;
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
                throw;
            }
        }
    }
}
