using Common.Entities.Fixes;
using Microsoft.AspNetCore.Mvc;
using Web.Blazor.Helpers;
using Web.Blazor.Providers;

namespace Web.Blazor.ControllersV1;

[Obsolete("Use V2 instead")]
[ApiController]
[Route("api/fixes")]
public sealed class FixesController : ControllerBase
{
    private readonly FixesProvider _fixesProvider;


    public FixesController(FixesProvider fixesProvider)
    {
        _fixesProvider = fixesProvider;
    }


    [Obsolete("Use V2 instead")]
    [HttpGet]
    public IEnumerable<FixesList> GetFixesList() => _fixesProvider.GetFixesList(0);


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
}

