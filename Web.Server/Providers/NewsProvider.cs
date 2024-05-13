using Common.Entities;
using System.Collections.Immutable;
using Web.Server.DbEntities;
using Web.Server.Helpers;

namespace Superheater.Web.Server.Providers
{
    public sealed class NewsProvider
    {
        private readonly ILogger<NewsProvider> _logger;
        private readonly DatabaseContextFactory _databaseContextFactory;


        public NewsProvider(
            ILogger<NewsProvider> logger,
            DatabaseContextFactory databaseContextFactory)
        {
            _logger = logger;
            _databaseContextFactory = databaseContextFactory;
        }


        public ImmutableList<NewsEntity> GetNews()
        {
            using var dbContext = _databaseContextFactory.Get();
            var newsEntities = dbContext.News;

            List<NewsEntity> news = new(newsEntities.Count());

            //adding news in reverse order
            for (var i = newsEntities.Count() - 1; i >= 0; i--)
            {
                var element = newsEntities.ElementAt(i);

                NewsEntity entity = new()
                {
                    Date = element.Date,
                    Content = element.Content
                };

                news.Add(entity);
            }

            return [.. news];
        }

        internal bool AddNews(Tuple<DateTime, string, string> message)
        {
            string apiPassword = Environment.GetEnvironmentVariable("ApiPass")!;

            if (!apiPassword.Equals(message.Item3))
            {
                return false;
            }

            using var dbContext = _databaseContextFactory.Get();

            NewsDbEntity entity = new()
            {
                Date = message.Item1,
                Content = message.Item2
            };

            dbContext.News.Add(entity);
            dbContext.SaveChanges();

            return true;
        }

        internal bool ChangeNews(Tuple<DateTime, string, string> message)
        {
            string apiPassword = Environment.GetEnvironmentVariable("ApiPass")!;

            if (!apiPassword.Equals(message.Item3))
            {
                return false;
            }

            using var dbContext = _databaseContextFactory.Get();

            NewsDbEntity? entity = null;
            
            foreach (var news in dbContext.News)
            {
                var date1 = message.Item1;
                var date2 = news.Date;

                if (date1 == date2)
                {
                    entity = news;
                    break;
                }
            }

            if (entity is null)
            {
                return false;
            }

            entity.Content = message.Item2;
            dbContext.SaveChanges();

            return true;
        }
    }
}
