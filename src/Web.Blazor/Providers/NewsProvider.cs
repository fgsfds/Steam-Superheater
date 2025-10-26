using Common.Axiom.Entities;
using Common.Axiom.Enums;
using Database.Server;
using Database.Server.DbEntities;
using Microsoft.EntityFrameworkCore;

namespace Web.Blazor.Providers;

public sealed class NewsProvider
{
    private readonly DatabaseContextFactory _databaseContextFactory;


    public NewsProvider(DatabaseContextFactory databaseContextFactory)
    {
        _databaseContextFactory = databaseContextFactory;
    }


    public List<NewsEntity>? GetNews(int version)
    {
        using var dbContext = _databaseContextFactory.Get();
        var newsEntities = dbContext.News.AsNoTracking().Where(x => x.TableVersion > version).OrderByDescending(static x => x.Date).ToList();

        if (newsEntities is null || newsEntities.Count == 0)
        {
            return null;
        }

        var news = newsEntities.ConvertAll(static x =>
            new NewsEntity()
            {
                Date = x.Date.ToUniversalTime(),
                Content = x.Content
            });

        return news;
    }

    /// <summary>
    /// Add news to the database
    /// </summary>
    /// <param name="date">News date</param>
    /// <param name="text">News text</param>
    /// <returns>News successfully added</returns>
    public bool AddNews(DateTime date, string text)
    {
        using var dbContext = _databaseContextFactory.Get();

        var databaseVersions = dbContext.DatabaseVersions.Find(DatabaseTableEnum.News)!;
        var newTableVersion = databaseVersions.Version + 1;

        NewsDbEntity entity = new()
        {
            Date = date.ToUniversalTime(),
            Content = text,
            TableVersion = newTableVersion,
        };

        _ = dbContext.News.Add(entity);

        _ = databaseVersions.Version = newTableVersion;

        _ = dbContext.SaveChanges();

        return true;
    }

    /// <summary>
    /// Change existing news
    /// </summary>
    /// <param name="date">News date</param>
    /// <param name="text">News text</param>
    /// <returns>News successfully changed</returns>
    public bool ChangeNews(DateTime date, string text)
    {
        using var dbContext = _databaseContextFactory.Get();

        var databaseVersions = dbContext.DatabaseVersions.Find(DatabaseTableEnum.News)!;
        var newTableVersion = databaseVersions.Version + 1;

        var entity = dbContext.News.Find(date);

        if (entity is null)
        {
            return false;
        }

        entity.Content = text;
        entity.TableVersion = newTableVersion;

        _ = databaseVersions.Version = newTableVersion;

        _ = dbContext.SaveChanges();

        return true;
    }
}
