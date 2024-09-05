using Common.Entities.Fixes;
using Common.Helpers;
using Microsoft.AspNetCore.Mvc;
using Web.Blazor.Helpers;
using Web.Blazor.Providers;
using static Web.Blazor.Providers.StatsProvider;

namespace Web.Blazor.Controllers;

[ApiController]
[Route("api/fixes")]
public sealed class FixesController : ControllerBase
{
    private readonly FixesProvider _fixesProvider;
    private readonly StatsProvider _statsProvider;
    private readonly ServerProperties _serverProperties;

    public FixesController(
        FixesProvider fixesProvider,
        StatsProvider statsProvider,
        ServerProperties serverProperties
        )
    {
        _fixesProvider = fixesProvider;
        _statsProvider = statsProvider;
        _serverProperties = serverProperties;
    }


    [HttpGet("last_updated")]
    public string GetLastUpdated() => DateTime.Now.ToString(Consts.DateTimeFormat);


    [HttpGet]
    public IEnumerable<FixesList> GetFixesList() => _fixesProvider.GetFixesList();


    [HttpGet("{guid:Guid}")]
    public bool CheckIfFixEsists(Guid guid) => _fixesProvider.CheckIfFixExists(guid);


    [HttpPut("installs/add")]
    public int AddNumberOfInstalls([FromBody] Guid guid) => _fixesProvider.IncreaseFixInstallsCount(guid);


    [HttpPut("score/change")]
    public async Task<int> ChangeScoreAsync([FromBody] Tuple<Guid, sbyte> message) => await _fixesProvider.ChangeFixScoreAsync(message.Item1, message.Item2);


    [HttpPost("report")]
    public async Task ReportFixAsync([FromBody] Tuple<Guid, string> message) => await _fixesProvider.AddReportAsync(message.Item1, message.Item2);


    [HttpPut("delete")]
    public StatusCodeResult ChangeFixState([FromBody] Tuple<Guid, bool, string> message)
    {
        var result = _fixesProvider.ChangeFixDisabledState(message.Item1, message.Item2, message.Item3);

        if (result)
        {
            return StatusCode(StatusCodes.Status200OK);
        }
        else
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }
    }


    [HttpPost("add")]
    public async Task<StatusCodeResult> AddFixToDbAsync([FromBody] Tuple<int, string, string, string> message)
    {
        var result = await _fixesProvider.AddFixAsync(message.Item1, message.Item2, message.Item3, message.Item4);

        if (result)
        {
            return StatusCode(StatusCodes.Status200OK);
        }
        else
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }
    }


    [HttpPost("check")]
    public async Task<StatusCodeResult> ForceCheckFixesAsync([FromBody] string password)
    {
        var result = await _fixesProvider.ForceCheckFixesAsync(password);

        if (result)
        {
            return StatusCode(StatusCodes.Status200OK);
        }
        else
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }
    }


    [HttpGet("stats")]
    public FixesStats? GetFixesStats() => _statsProvider.Stats;
}

