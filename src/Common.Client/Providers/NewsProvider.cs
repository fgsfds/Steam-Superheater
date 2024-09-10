using Api.Common.Interface;
using Common.Entities;
using Common.Enums;
using Database.Client;
using System.Text.Json;

namespace Common.Client.Providers;

public sealed class NewsProvider
{
    private const byte NewsPerPage = 5;

    private readonly ApiInterface _apiInterface;
    private readonly GamesProvider _gamesProvider;
    private readonly DatabaseContextFactory _dbContextFactory;
    private readonly IConfigProvider _config;
    private readonly Logger _logger;

    private Dictionary<int, List<NewsEntity>> _newsEntitiesPages = [];

    public int PagesCount => _newsEntitiesPages.Count;

    public int UnreadNewsCount { get; private set; }

    public bool HasUnreadNews => UnreadNewsCount > 0;


    public NewsProvider(
        ApiInterface apiInterface,
        GamesProvider gamesProvider,
        DatabaseContextFactory dbContextFactory,
        IConfigProvider configProvider,
        Logger logger
        )
    {
        _apiInterface = apiInterface;
        _gamesProvider = gamesProvider;
        _dbContextFactory = dbContextFactory;
        _config = configProvider;
        _logger = logger;
    }


    /// <summary>
    /// Get list of news from online or local repo
    /// </summary>
    public async Task<Result> UpdateNewsListAsync()
    {
        using var dbContext = _dbContextFactory.Get();

        var newsCacheDbEntity = dbContext.Cache.Find(DatabaseTableEnum.News)!;
        var currentNewsVersion = newsCacheDbEntity.Version!;
        var currentNewsList = JsonSerializer.Deserialize(newsCacheDbEntity.Data, NewsEntityContext.Default.ListNewsEntity)!;

        var newNewsList = await _apiInterface.GetNewsListAsync(currentNewsVersion).ConfigureAwait(false);

        if (newNewsList.IsSuccess && newNewsList.ResultObject?.Version > currentNewsVersion)
        {
            _logger.Info("Getting online news");

            currentNewsList = [.. newNewsList.ResultObject.News.Concat(currentNewsList)];
            newsCacheDbEntity.Version = newNewsList.ResultObject.Version;
            newsCacheDbEntity.Data = JsonSerializer.Serialize(currentNewsList, NewsEntityContext.Default.ListNewsEntity);

            _ = await dbContext.SaveChangesAsync().ConfigureAwait(false);
        }

        _newsEntitiesPages = [];

        byte page = 1;

        while (currentNewsList.Count > 0)
        {
            var elements = currentNewsList.Take(NewsPerPage).ToList();

            _newsEntitiesPages.Add(page, elements);

            currentNewsList.RemoveRange(0, elements.Count);

            page++;
        }

        UpdateReadStatusOfExistingNews();

        return new(newNewsList.ResultEnum, newNewsList.Message);
    }

    public List<NewsEntity> GetNewsPage(int page) => _newsEntitiesPages[page];



    /// <summary>
    /// Change content of the existing news
    /// </summary>
    /// <param name="date">Date of the news</param>
    /// <param name="content">Content</param>
    public async Task<Result> ChangeNewsContentAsync(
        DateTime date,
        string content
        )
    {
        var result1 = await _apiInterface.ChangeNewsAsync(date, content).ConfigureAwait(false);

        if (!result1.IsSuccess)
        {
            return result1;
        }

        var result2 = await UpdateNewsListAsync().ConfigureAwait(false);

        return result2;
    }

    /// <summary>
    /// Add news
    /// </summary>
    /// <param name="content">News content</param>
    public async Task<Result> AddNewsAsync(string content)
    {
        var result1 = await _apiInterface.AddNewsAsync(content).ConfigureAwait(false);

        if (!result1.IsSuccess)
        {
            return result1;
        }

        var result2 = await UpdateNewsListAsync().ConfigureAwait(false);

        return result2;
    }

    /// <summary>
    /// Mark all news as read
    /// </summary>
    public void MarkAllAsRead()
    {
        UpdateConfigLastReadVersion();

        UpdateReadStatusOfExistingNews();
    }

    /// <summary>
    /// Update last read date in config
    /// </summary>
    private void UpdateConfigLastReadVersion()
    {
        var lastReadDate = _newsEntitiesPages[1].First().Date + TimeSpan.FromSeconds(1);

        _config.LastReadNewsDate = lastReadDate;
    }

    /// <summary>
    /// Set read status based on last read date from config
    /// </summary>
    private void UpdateReadStatusOfExistingNews()
    {
        UnreadNewsCount = 0;

        foreach (var item in _newsEntitiesPages)
        {
            foreach (var news in item.Value)
            {
                news.IsNewer = false;

                if (news.Date > _config.LastReadNewsDate)
                {
                    news.IsNewer = true;
                    UnreadNewsCount++;
                }
            }
        }
    }
}

