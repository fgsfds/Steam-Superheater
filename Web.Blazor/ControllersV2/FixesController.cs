using Api.Common.Messages;
using Common.Entities.Fixes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Web.Blazor.Providers;
using Microsoft.AspNetCore.Http;

namespace Web.Blazor.ControllersV2;

[ApiController]
[Route("api2/fixes")]
public sealed class FixesController : ControllerBase
{
    private readonly FixesProvider _fixesProvider;


    public FixesController(FixesProvider fixesProvider)
    {
        _fixesProvider = fixesProvider;
    }


    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<FixesList>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<FixesList>> GetFixesList([FromQuery] int v = 0)
    {
        var news = _fixesProvider.GetFixesList(v);

        return Ok(news);
    }


    [HttpGet("exists")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<CheckIfFixExistsOutMessage> CheckIfFixExists([FromQuery] Guid? guid)
    {
        if (guid is null)
        {
            return BadRequest();
        }

        var currentVersion = _fixesProvider.CheckIfFixExists(guid.Value);

        if (currentVersion is null)
        {
            return NotFound();
        }

        CheckIfFixExistsOutMessage result = new()
        {
            CurrentVersion = currentVersion
        };

        return Ok(result);
    }


    [HttpPut("installs/add")]
    [ProducesResponseType(typeof(IncreaseInstallsCountOutMessage), StatusCodes.Status200OK)]
    public ActionResult<IncreaseInstallsCountOutMessage> AddNumberOfInstalls([FromBody] IncreaseInstallsCountInMessage message)
    {
        var installsCount = _fixesProvider.IncreaseFixInstallsCount(message.FixGuid);

        IncreaseInstallsCountOutMessage result = new() { InstallsCount = installsCount };

        return Ok(result);
    }


    [HttpPut("score/change")]
    [ProducesResponseType(typeof(ChangeScoreOutMessage), StatusCodes.Status200OK)]
    public async Task<ActionResult<ChangeScoreOutMessage>> ChangeScoreAsync([FromBody] ChangeScoreInMessage message)
    {
        var score = await _fixesProvider.ChangeFixScoreAsync(message.FixGuid, message.Increment);

        ChangeScoreOutMessage result = new() { Score = score };

        return Ok(result);
    }


    [HttpPost("report")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<StatusCodeResult> ReportFixAsync([FromBody] ReportFixInMessage message)
    {
        var result = await _fixesProvider.AddReportAsync(message.FixGuid, message.Text);

        if (result)
        {
            return Ok();
        }
        else
        {
            return StatusCode(500);
        }
    }


    [Authorize]
    [HttpPut("state")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public StatusCodeResult ChangeFixState([FromBody] ChangeFixStateInMessage message)
    {
        var result = _fixesProvider.ChangeFixDisabledState(message.FixGuid, message.IsDisabled);

        if (result)
        {
            return Ok();
        }
        else
        {
            return StatusCode(500);
        }
    }


    [Authorize]
    [HttpPost("add")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<StatusCodeResult> AddFixToDbAsync([FromBody] AddFixInMessage message)
    {
        var result = await _fixesProvider.AddFixAsync(message.GameId, message.GameName, message.Fix);

        if (result)
        {
            return Ok();
        }
        else
        {
            return StatusCode(500);
        }
    }


    [Authorize]
    [HttpPost("check")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesDefaultResponseType]
    public async Task<StatusCodeResult> ForceCheckFixesAsync()
    {
        var result = await _fixesProvider.ForceCheckFixesAsync();

        if (result)
        {
            return Ok();
        }
        else
        {
            return StatusCode(500);
        }
    }
}
