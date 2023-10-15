using Common.Config;
using Common.Entities;
using Common.Helpers;
using System.Collections.Immutable;
using System.Xml.Serialization;

namespace Common.Providers
{
    public sealed class NewsProvider
    {
        private readonly ConfigEntity _config;

        public NewsProvider(ConfigProvider config)
        {
            _config = config.Config;
        }

        /// <summary>
        /// Get list of news
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NullReferenceException"></exception>
        public async Task<ImmutableList<NewsEntity>> GetNewsListAsync()
        {
            string? news;

            if (_config.UseLocalRepo)
            {
                var file = Path.Combine(CommonProperties.LocalRepoPath, Consts.NewsFile);

                if (!File.Exists(file))
                {
                    throw new FileNotFoundException(file);
                }

                news = File.ReadAllText(file);
            }
            else
            {
                news = await DownloadNewsXMLAsync();
            }

            XmlSerializer xmlSerializer = new(typeof(List<NewsEntity>));

            ImmutableList<NewsEntity>? newsList;

            try
            {
                using (StringReader fs = new(news))
                {
                    newsList = (xmlSerializer.Deserialize(fs) as List<NewsEntity>).ToImmutableList();
                }
            }
            catch (Exception ex)
            {
                Logger.Log("Error while deserializing news...");
                Logger.Log(ex.Message);

                return ImmutableList.Create<NewsEntity>();
            }

            if (newsList is null)
            {
                throw new NullReferenceException(nameof(newsList));
            }

            return newsList.OrderByDescending(x => x.Version).ToImmutableList();
        }

        /// <summary>
        /// Get list of news from online repository
        /// </summary>
        /// <returns></returns>
        private async Task<string> DownloadNewsXMLAsync()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(10);
                    using var stream = await client.GetStreamAsync(CommonProperties.CurrentFixesRepo + Consts.NewsFile);
                    using var file = new StreamReader(stream);
                    var newsXml = await file.ReadToEndAsync();

                    return newsXml;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
