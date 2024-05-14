using Common.Entities;
using Microsoft.EntityFrameworkCore;
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


        public List<NewsEntity> GetNews()
        {
            using var dbContext = _databaseContextFactory.Get();
            var newsEntities = dbContext.News.AsNoTracking().OrderByDescending(static x => x.Date).ToList();

            var news = newsEntities.ConvertAll(x =>
                new NewsEntity()
                {
                    Date = x.Date.ToUniversalTime(),
                    Content = x.Content
                }
            );

            return news;
        }

        internal bool AddNews(Tuple<DateTime, string, string> message)
        {
            var apiPassword = Environment.GetEnvironmentVariable("ApiPass")!;

            if (!apiPassword.Equals(message.Item3))
            {
                return false;
            }

            using var dbContext = _databaseContextFactory.Get();

            NewsDbEntity entity = new()
            {
                Date = message.Item1.ToUniversalTime(),
                Content = message.Item2
            };

            dbContext.News.Add(entity);
            dbContext.SaveChanges();

            return true;
        }

        internal bool ChangeNews(Tuple<DateTime, string, string> message)
        {
            var apiPassword = Environment.GetEnvironmentVariable("ApiPass")!;

            if (!apiPassword.Equals(message.Item3))
            {
                return false;
            }

            using var dbContext = _databaseContextFactory.Get();

            var entity = dbContext.News.Find(message.Item1);

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
