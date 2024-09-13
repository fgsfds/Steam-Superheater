using Common.Entities;

namespace Common.Client.Providers.Interfaces;
public interface INewsProvider
{
    bool HasUnreadNews { get; }
    int PagesCount { get; }
    int UnreadNewsCount { get; }

    /// <summary>
    /// Get list of news from online or local repo
    /// </summary>
    Task<Result> UpdateNewsListAsync();

    List<NewsEntity> GetNewsPage(int page);

    /// <summary>
    /// Mark all news as read
    /// </summary>
    void MarkAllAsRead();

    /// <summary>
    /// Add news
    /// </summary>
    /// <param name="content">News content</param>
    Task<Result> AddNewsAsync(string content);

    /// <summary>
    /// Change content of the existing news
    /// </summary>
    /// <param name="date">Date of the news</param>
    /// <param name="content">Content</param>
    Task<Result> ChangeNewsContentAsync(DateTime date, string content);
}