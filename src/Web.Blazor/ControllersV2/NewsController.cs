using Api.Axiom.Messages;
using Common.Axiom.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Web.Blazor.Providers;

namespace Web.Blazor.ControllersV2;

[ApiController]
[Route("api2/news")]
public sealed class NewsController : ControllerBase
{
    private readonly NewsProvider _newsProvider;
    private readonly DatabaseVersionsProvider _databaseVersionsProvider;


    public NewsController(
        NewsProvider newsProvider,
        DatabaseVersionsProvider databaseVersionsProvider
        )
    {
        _newsProvider = newsProvider;
        _databaseVersionsProvider = databaseVersionsProvider;
    }


    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesDefaultResponseType]
    public ActionResult<GetNewsOutMessage> GetNewsList([FromQuery] int v = 0)
    {
        var version = _databaseVersionsProvider.GetDatabaseVersions()[DatabaseTableEnum.News];

        if (v >= version)
        {
            return NoContent();
        }

        var news = _newsProvider.GetNews(v);

        if (news is null or [])
        {
            return NoContent();
        }

        GetNewsOutMessage result = new()
        {
            News = news,
            Version = version
        };

        return Ok(result);
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
