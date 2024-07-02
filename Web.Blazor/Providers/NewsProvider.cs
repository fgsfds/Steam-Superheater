using Common.Entities;
using Microsoft.EntityFrameworkCore;
using Web.Blazor.DbEntities;
using Web.Blazor.Helpers;

namespace Web.Blazor.Providers
{
    public sealed class NewsProvider
    {
        private readonly DatabaseContextFactory _databaseContextFactory;

        public List<NewsEntity>? News { get; private set; }


        public NewsProvider(
            DatabaseContextFactory databaseContextFactory)
        {
            _databaseContextFactory = databaseContextFactory;

            UpdateNews();
        }

        /// <summary>
        /// Add news to the database
        /// </summary>
        /// <param name="date">News date</param>
        /// <param name="text">News text</param>
        /// <param name="password">API password</param>
        /// <returns>News successfully added</returns>
        public bool AddNews(DateTime date, string text, string password)
        {
            var apiPassword = Environment.GetEnvironmentVariable("ApiPass")!;

            if (!apiPassword.Equals(password))
            {
                return false;
            }

            using var dbContext = _databaseContextFactory.Get();

            NewsDbEntity entity = new()
            {
                Date = date.ToUniversalTime(),
                Content = text
            };

            dbContext.News.Add(entity);
            dbContext.SaveChanges();

            UpdateNews();

            return true;
        }

        /// <summary>
        /// Change existing news
        /// </summary>
        /// <param name="date">News date</param>
        /// <param name="text">News text</param>
        /// <param name="password">API password</param>
        /// <returns>News successfully changed</returns>
        public bool ChangeNews(DateTime date, string text, string password)
        {
            var apiPassword = Environment.GetEnvironmentVariable("ApiPass")!;

            if (!apiPassword.Equals(password))
            {
                return false;
            }

            using var dbContext = _databaseContextFactory.Get();

            var entity = dbContext.News.Find(date);

            if (entity is null)
            {
                return false;
            }

            entity.Content = text;
            dbContext.SaveChanges();

            UpdateNews();

            return true;
        }


        /// <summary>
        /// Get list of news from the database
        /// </summary>
        private void UpdateNews()
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

            News = news;
        }
    }
}
