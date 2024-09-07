using Common.Entities.Fixes;
using Common.Helpers;
using Microsoft.AspNetCore.Mvc;
using Web.Blazor.Helpers;
using Web.Blazor.Providers;
using static Web.Blazor.Providers.StatsProvider;

namespace Web.Blazor.ControllersV1;

[Obsolete("Use V2 instead")]
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


    [Obsolete("Use V2 instead")]
    [HttpGet("last_updated")]
    public string GetLastUpdated() => DateTime.Now.ToString(Consts.DateTimeFormat);


    [Obsolete("Use V2 instead")]
    [HttpGet]
    public IEnumerable<FixesList> GetFixesList() => _fixesProvider.GetFixesList();


    [Obsolete("Use V2 instead")]
    [HttpGet("{guid:Guid}")]
    public bool CheckIfFixEsists(Guid guid) => _fixesProvider.CheckIfFixExists(guid);


    [Obsolete("Use V2 instead")]
    [HttpPut("installs/add")]
    public int AddNumberOfInstalls([FromBody] Guid guid) => _fixesProvider.IncreaseFixInstallsCount(guid);


    [Obsolete("Use V2 instead")]
    [HttpPut("score/change")]
    public async Task<int> ChangeScoreAsync([FromBody] Tuple<Guid, sbyte> message) => await _fixesProvider.ChangeFixScoreAsync(message.Item1, message.Item2);


    [Obsolete("Use V2 instead")]
    [HttpPost("report")]
    public async Task ReportFixAsync([FromBody] Tuple<Guid, string> message) => await _fixesProvider.AddReportAsync(message.Item1, message.Item2);


    [Obsolete("Use V2 instead")]
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


    [Obsolete("Use V2 instead")]
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


    [Obsolete("Use V2 instead")]
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


    [Obsolete("Use V2 instead")]
    [HttpGet("stats")]
    public FixesStats? GetFixesStats() => _statsProvider.Stats;
}

