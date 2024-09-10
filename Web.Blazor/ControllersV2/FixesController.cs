using Api.Common.Messages;
using Common.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Web.Blazor.Providers;

namespace Web.Blazor.ControllersV2;

[ApiController]
[Route("api2/fixes")]
public sealed class FixesController : ControllerBase
{
    private readonly FixesProvider _fixesProvider;
    private readonly DatabaseVersionsProvider _databaseVersionsProvider;


    public FixesController(
        FixesProvider fixesProvider,
        DatabaseVersionsProvider databaseVersionsProvider
        )
    {
        _fixesProvider = fixesProvider;
        _databaseVersionsProvider = databaseVersionsProvider;
    }


    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public ActionResult<GetFixesOutMessageContext> GetFixesList([FromQuery] int v = 0)
    {
        var version = _databaseVersionsProvider.GetDatabaseVersions()[DatabaseTableEnum.Fixes];

        if (v >= version)
        {
            return NoContent();
        }

        var fixes = _fixesProvider.GetFixesList(v);

        if (fixes is null or [])
        {
            return NoContent();
        }

        GetFixesOutMessage result = new()
        {
            Fixes = fixes,
            Version = version
        };

        return Ok(result);
    }


    [HttpGet("exists")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public ActionResult<CheckIfFixExistsOutMessage> CheckIfFixExists([FromQuery] Guid? guid)
    {
        if (guid is null)
        {
            return BadRequest();
        }

        var currentVersion = _fixesProvider.CheckIfFixExists(guid.Value);

        if (currentVersion is null)
        {
            return NoContent();
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


    [HttpPut("score")]
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
}
