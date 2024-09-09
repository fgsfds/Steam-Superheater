using Api.Common.Messages;
using Common.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Web.Blazor.Providers;

namespace Web.Blazor.ControllersV2;

[ApiController]
[Route("api2/news")]
public sealed class NewsController : ControllerBase
{
    private readonly NewsProvider _newsProvider;


    public NewsController(NewsProvider newsProvider)
    {
        _newsProvider = newsProvider;
    }


    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<List<NewsEntity>> GetNewsList([FromQuery] int v = 0)
    {
        var news = _newsProvider.GetNews(v);

        return Ok(news);
    }

    [Authorize]
    [HttpPost("add")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public StatusCodeResult AddNews([FromBody] AddChangeNewsInMessage message)
    {
        var result = _newsProvider.AddNews(message.Date, message.Content);

        if (result)
        {
            return StatusCode(StatusCodes.Status200OK);
        }
        else
        {
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    [Authorize]
    [HttpPut("change")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public StatusCodeResult ChangeNews([FromBody] AddChangeNewsInMessage message)
    {
        var result = _newsProvider.ChangeNews(message.Date, message.Content);

        if (result)
        {
            return StatusCode(StatusCodes.Status200OK);
        }
        else
        {
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}
