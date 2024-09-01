using Common.Client.API;
using Common.Client.Config;
using Common.Entities;
using System.Collections.Immutable;

namespace Common.Client.Models;

public sealed class NewsModel
{
    private readonly IConfigProvider _config;
    private readonly ApiInterface _newsProvider;

    public int UnreadNewsCount => _news.Count(static x => x.IsNewer);

    public bool HasUnreadNews => UnreadNewsCount > 0;

    private List<NewsEntity> _news = [];


    public NewsModel(
        IConfigProvider config,
        ApiInterface newsProvider
        )
    {
        _config = config;
        _newsProvider = newsProvider;
    }

    public ImmutableList<NewsEntity> GetNews() => [.. _news];


    /// <summary>
    /// Get list of news from online or local repo
    /// </summary>
    public async Task<Result> UpdateNewsListAsync()
    {
        var result = await _newsProvider.GetNewsListAsync().ConfigureAwait(false);

        if (result.IsSuccess)
        {
            _news = [.. result.ResultObject];

            UpdateReadStatusOfExistingNews();
        }

        return new(result.ResultEnum, result.Message);
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
    /// Change content of the existing news
    /// </summary>
    /// <param name="date">Date of the news</param>
    /// <param name="content">Content</param>
    public async Task<Result> ChangeNewsContentAsync(
        DateTime date,
        string content
        )
    {
        var result1 = await _newsProvider.ChangeNewsAsync(date, content).ConfigureAwait(false);

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
        var result1 = await _newsProvider.AddNewsAsync(content).ConfigureAwait(false);

        if (!result1.IsSuccess)
        {
            return result1;
        }

        var result2 = await UpdateNewsListAsync().ConfigureAwait(false);

        return result2;
    }

    /// <summary>
    /// Set read status based on last read date from config
    /// </summary>
    private void UpdateReadStatusOfExistingNews()
    {
        foreach (var item in _news)
        {
            item.IsNewer = item.Date > _config.LastReadNewsDate;
        }
    }

    /// <summary>
    /// Update last read date in config
    /// </summary>
    private void UpdateConfigLastReadVersion()
    {
        var lastReadDate = _news.Max(static x => x.Date) + TimeSpan.FromSeconds(1);

        _config.LastReadNewsDate = lastReadDate;
    }
}

