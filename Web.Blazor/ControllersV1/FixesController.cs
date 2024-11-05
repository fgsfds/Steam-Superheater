using Common.Entities.Fixes;
using Common.Enums;
using Microsoft.AspNetCore.Mvc;
using Web.Blazor.Providers;

namespace Web.Blazor.ControllersV1;

[Obsolete("Use V2 instead")]
[ApiController]
[Route("api/fixes")]
public sealed class FixesController : ControllerBase
{
    private readonly FixesProvider _fixesProvider;
    private readonly EventsProvider _eventsProvider;


    public FixesController(
        FixesProvider fixesProvider,
        EventsProvider eventsProvider
        )
    {
        _fixesProvider = fixesProvider;
        _eventsProvider = eventsProvider;
    }


    [Obsolete("Use V2 instead")]
    [HttpGet]
    public IEnumerable<FixesList> GetFixesList()
    {
        _ = _eventsProvider.LogEventAsync(EventTypeEnum.GetFixes, new(1, 2, 0), null);

        return _fixesProvider.GetFixesList(0, new Version(1, 2, 0));
    }

    [Obsolete("Use V2 instead")]
    [HttpGet("{guid:Guid}")]
    public bool CheckIfFixEsists(Guid guid) => _fixesProvider.CheckIfFixExists(guid) is not null;


    [Obsolete("Use V2 instead")]
    [HttpPut("installs/add")]
    public int AddNumberOfInstalls([FromBody] Guid guid)
    {
        _ = _eventsProvider.LogEventAsync(EventTypeEnum.GetFixes, new(1, 2, 0), guid);

        return _fixesProvider.IncreaseFixInstallsCount(guid);
    }

    [Obsolete("Use V2 instead")]
    [HttpPut("score/change")]
    public async Task<int> ChangeScoreAsync([FromBody] Tuple<Guid, sbyte> message) => await _fixesProvider.ChangeFixScoreAsync(message.Item1, message.Item2);


    [Obsolete("Use V2 instead")]
    [HttpPost("report")]
    public async Task ReportFixAsync([FromBody] Tuple<Guid, string> message) => await _fixesProvider.AddReportAsync(message.Item1, message.Item2);
}

