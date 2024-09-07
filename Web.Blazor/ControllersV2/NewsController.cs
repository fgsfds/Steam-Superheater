using Common.Entities;
using Microsoft.AspNetCore.Mvc;
using Web.Blazor.Providers;

namespace Web.Blazor.ControllersV2;

[ApiController]
[Route("api2/news")]
public sealed class NewsController : ControllerBase
{
    private readonly NewsProvider _newsProvider;

    public NewsController(
        NewsProvider newsProvider
        )
    {
        _newsProvider = newsProvider;
    }

    [HttpGet]
    public List<NewsEntity>? GetNewsList() => _newsProvider.News;

    [HttpPost("add")]
    public StatusCodeResult AddNews([FromBody] Tuple<DateTime, string, string> message)
    {
        var result = _newsProvider.AddNews(message.Item1, message.Item2, message.Item3);

        if (result)
        {
            return StatusCode(StatusCodes.Status200OK);
        }
        else
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }
    }

    [HttpPut("change")]
    public StatusCodeResult ChangeNews([FromBody] Tuple<DateTime, string, string> message)
    {
        var result = _newsProvider.ChangeNews(message.Item1, message.Item2, message.Item3);

        if (result)
        {
            return StatusCode(StatusCodes.Status200OK);
        }
        else
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }
    }
}

