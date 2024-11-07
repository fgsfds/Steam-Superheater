using Common.Entities.Fixes;
using Common.Enums;
using Microsoft.AspNetCore.Mvc;
using Web.Blazor.Providers;

namespace Web.Blazor.ControllersV1;

[Obsolete("Remove when there's no versions <2.0.0")]
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


    [Obsolete("Remove when there's no versions <2.0.0")]
    [HttpGet]
    public IEnumerable<FixesList> GetFixesList()
    {
        _ = _eventsProvider.LogEventAsync(EventTypeEnum.GetFixes, new(1, 2, 0), null);

        return _fixesProvider.GetFixesList(0, new Version(1, 2, 0));
    }

    [Obsolete("Remove when there's no versions <2.0.0")]
    [HttpGet("{guid:Guid}")]
    public bool CheckIfFixExists(Guid guid) => _fixesProvider.CheckIfFixExists(guid) is not null;


    [Obsolete("Remove when there's no versions <2.0.0")]
    [HttpPut("installs/add")]
    public int AddNumberOfInstalls([FromBody] Guid guid)
    {
        _ = _eventsProvider.LogEventAsync(EventTypeEnum.GetFixes, new(1, 2, 0), guid);

        return _fixesProvider.IncreaseFixInstallsCount(guid);
    }

    [Obsolete("Remove when there's no versions <2.0.0")]
    [HttpPut("score/change")]
    public async Task<int> ChangeScoreAsync([FromBody] Tuple<Guid, sbyte> message) => await _fixesProvider.ChangeFixScoreAsync(message.Item1, message.Item2);


    [Obsolete("Remove when there's no versions <2.0.0")]
    [HttpPost("report")]
    public async Task ReportFixAsync([FromBody] Tuple<Guid, string> message) => await _fixesProvider.AddReportAsync(message.Item1, message.Item2);
}

