using System.Text.Json;
using Api.Axiom.Interfaces;
using Common.Axiom;
using Common.Axiom.Entities;
using Common.Axiom.Enums;
using Common.Client.Providers.Interfaces;
using Database.Client;
using Microsoft.Extensions.Logging;

namespace Common.Client.Providers;

public sealed class NewsProvider : INewsProvider
{
    private const byte NewsPerPage = 5;

    private readonly IApiInterface _apiInterface;
    private readonly DatabaseContextFactory _dbContextFactory;
    private readonly IConfigProvider _config;
    private readonly ILogger _logger;

    private Dictionary<int, List<NewsEntity>> _newsEntitiesPages = [];

    public int PagesCount => _newsEntitiesPages.Count;

    public int UnreadNewsCount { get; private set; }

    public bool HasUnreadNews => UnreadNewsCount > 0;


    public NewsProvider(
        IApiInterface apiInterface,
        DatabaseContextFactory dbContextFactory,
        IConfigProvider configProvider,
        ILogger logger
        )
    {
        _apiInterface = apiInterface;
        _dbContextFactory = dbContextFactory;
        _config = configProvider;
        _logger = logger;
    }


    /// <inheritdoc/>
    public async Task<Result> UpdateNewsListAsync()
    {
        _newsEntitiesPages.Clear();

        await using var dbContext = _dbContextFactory.Get();

        var newsCacheDbEntity = dbContext.Cache.Find(DatabaseTableEnum.News)!;
        var currentNewsVersion = newsCacheDbEntity.Version!;
        var currentNewsList = JsonSerializer.Deserialize(newsCacheDbEntity.Data, NewsListEntityContext.Default.ListNewsEntity)!;

        ResultEnum result = ResultEnum.Success;
        string resultMessage = string.Empty;

        if (ClientProperties.IsOfflineMode)
        {
            var newNewsList = File.ReadAllText(Path.Combine("..", "..", "..", "..", "db", "news.json"));
            currentNewsList = JsonSerializer.Deserialize(newNewsList, NewsListEntityContext.Default.ListNewsEntity)!;
        }
        else
        {
            var newNewsList = await _apiInterface.GetNewsListAsync(currentNewsVersion).ConfigureAwait(false);

            if (newNewsList.IsSuccess && newNewsList.ResultObject?.Version == 0)
            {
                currentNewsList = [];
                currentNewsVersion = -1;
            }

            if (newNewsList.IsSuccess && newNewsList.ResultObject?.Version > currentNewsVersion)
            {
                _logger.LogInformation("Getting online news");

                var newNewsDates = newNewsList.ResultObject.News.Select(x => x.Date);

                foreach (var item in currentNewsList.ToList())
                {
                    if (newNewsDates.Contains(item.Date))
                    {
                        _ = currentNewsList.Remove(item);
                    }
                }

                currentNewsList = [.. newNewsList.ResultObject.News.Concat(currentNewsList)];
                newsCacheDbEntity.Version = newNewsList.ResultObject.Version;
                newsCacheDbEntity.Data = JsonSerializer.Serialize(currentNewsList, NewsListEntityContext.Default.ListNewsEntity);

                _ = await dbContext.SaveChangesAsync().ConfigureAwait(false);
            }

            _newsEntitiesPages = [];

            result = newNewsList.ResultEnum;
            resultMessage = newNewsList.Message;
        }

        byte page = 1;

        while (currentNewsList.Count > 0)
        {
            var elements = currentNewsList.Take(NewsPerPage).ToList();

            _newsEntitiesPages.Add(page, elements);

            currentNewsList.RemoveRange(0, elements.Count);

            page++;
        }

        UpdateReadStatusOfExistingNews();

        return new(result, resultMessage);
    }

    /// <inheritdoc/>
    public List<NewsEntity> GetNewsPage(int page) => _newsEntitiesPages.TryGetValue(page, out var result) ? result : [];

    /// <inheritdoc/>
    public async Task<Result> ChangeNewsContentAsync(
        DateTime date,
        string content
        )
    {
        var result = await _apiInterface.ChangeNewsAsync(date, content).ConfigureAwait(false);

        return result;
    }

    /// <inheritdoc/>
    public async Task<Result> AddNewsAsync(string content)
    {
        var result = await _apiInterface.AddNewsAsync(content).ConfigureAwait(false);

        return result;
    }

    /// <inheritdoc/>
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

