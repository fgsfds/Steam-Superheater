using Common.Axiom.Entities;
using Microsoft.AspNetCore.Mvc;
using Web.Blazor.Providers;

namespace Web.Blazor.ControllersV1;

[Obsolete("Remove when there's no versions <2.0.0")]
[ApiController]
[Route("api/news")]
public sealed class NewsController : ControllerBase
{
    private readonly NewsProvider _newsProvider;


    public NewsController(NewsProvider newsProvider)
    {
        _newsProvider = newsProvider;
    }


    [Obsolete("Remove when there's no versions <2.0.0")]
    [HttpGet]
    public List<NewsEntity>? GetNewsList() => _newsProvider.GetNews(0);
}

