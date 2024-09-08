using Api.Common.Messages;
using Microsoft.AspNetCore.Mvc;
using Web.Blazor.Providers;

namespace Web.Blazor.ControllersV2;

[ApiController]
[Route("api2/db")]
public sealed class DatabaseController : ControllerBase
{
    private readonly DatabaseVersionsProvider _databaseVersionsProvider;


    public DatabaseController(DatabaseVersionsProvider databaseVersionsProvider)
    {
        _databaseVersionsProvider = databaseVersionsProvider;
    }


    [HttpGet("versions")]
    [ProducesResponseType(typeof(DatabaseVersionsOutMessage), StatusCodes.Status200OK)]
    public ActionResult<DatabaseVersionsOutMessage> GetLastUpdated()
    {
        var versions = _databaseVersionsProvider.GetDatabaseVersions();

        DatabaseVersionsOutMessage result = new() { Versions = versions };

        return Ok(result);
    }
}
