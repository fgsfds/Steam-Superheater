using Common.Config;
using Common.Entities;
using Common.Helpers;
using System.Collections.Immutable;
using System.Xml.Serialization;

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
            string? news;

            if (_config.UseLocalRepo)
            {
                var file = Path.Combine(CommonProperties.LocalRepoPath, Consts.NewsFile);

                if (!File.Exists(file))
                {
                    ThrowHelper.FileNotFoundException(file);
                }

                news = File.ReadAllText(file);
            }
            else
            {
                news = await DownloadNewsXMLAsync();
            }

            XmlSerializer xmlSerializer = new(typeof(List<NewsEntity>));

            using (StringReader fs = new(news))
            {
                if (xmlSerializer.Deserialize(fs) is not List<NewsEntity> list)
                {
                    Logger.Log("Error while deserializing news...");

                    return [];
                }

                return list.OrderByDescending(x => x.Date).ToImmutableList();
            }
        }

        /// <summary>
        /// Get list of news from online repository
        /// </summary>
        /// <returns></returns>
        private static async Task<string> DownloadNewsXMLAsync()
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
