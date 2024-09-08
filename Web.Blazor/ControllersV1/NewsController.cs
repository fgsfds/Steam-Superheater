using Common.Entities;
using Microsoft.AspNetCore.Mvc;
using Web.Blazor.Providers;

namespace Web.Blazor.ControllersV1;

[Obsolete("Use V2 instead")]
[ApiController]
[Route("api/news")]
public sealed class NewsController : ControllerBase
{
    private readonly NewsProvider _newsProvider;


    public NewsController(NewsProvider newsProvider)
    {
        _newsProvider = newsProvider;
    }


    [Obsolete("Use V2 instead")]
    [HttpGet]
    public List<NewsEntity>? GetNewsList() => _newsProvider.GetNews(0);
}

