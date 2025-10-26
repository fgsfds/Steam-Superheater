using Common.Axiom;
using Common.Axiom.Entities;
using Common.Client.Providers.Interfaces;

namespace Common.Client.Providers.Fakes;

public sealed class NewsProviderFake : INewsProvider
{
    public bool HasUnreadNews => true;

    public int PagesCount => 1;

    public int UnreadNewsCount => 2;

    public List<NewsEntity> GetNewsPage(int page)
    {
        NewsEntity news = new()
        {
            Content = """
            ### Added:

            **Faery - Legends of Avalon**: Official Patch

            **Syberia**: DDrawCompat

            **Homefront**: PhysX Fix

            **Sonic CD**: Decompilation Port

            #### No Intro Fixes for:

            Sid Meier's Pirates!, Yesterday, Duke Nukem Forever, NecroVisioN: Lost Company, Syberia, Syberia 2, BioShock Infinite, Homefront
            """,

            Date = DateTime.Now,
            IsNewer = true
        };

        NewsEntity news2 = new()
        {
            Content = """
            ### Added:

            **DOOM + DOOM II:** Nugget Doom, Nugget Doom Additions
            """,
            Date = DateTime.Now
        };

        return [news, news2];
    }

    public Task<Result> AddNewsAsync(string content)
    {
        return new(() => new(ResultEnum.Success, ""));
    }

    public Task<Result> ChangeNewsContentAsync(DateTime date, string content)
    {
        return new(() => new(ResultEnum.Success, ""));
    }

    public void MarkAllAsRead()
    {
        return;
    }

    public Task<Result> UpdateNewsListAsync()
    {
        return new(() => new(ResultEnum.Success, ""));
    }
}
