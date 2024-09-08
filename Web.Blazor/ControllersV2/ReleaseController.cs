using Api.Messages;
using Microsoft.AspNetCore.Mvc;
using Web.Blazor.Providers;

namespace Web.Blazor.ControllersV2;

[ApiController]
[Route("api2/releases")]
public sealed class ReleaseController : ControllerBase
{
    private readonly AppReleasesProvider _appReleasesProvider;


    public ReleaseController(AppReleasesProvider appReleasesProvider)
    {
        _appReleasesProvider = appReleasesProvider;
    }


    [HttpGet]
    [ProducesResponseType(typeof(GetReleasesOutMessage), StatusCodes.Status200OK)]
    public ActionResult<GetReleasesOutMessage> GetReleases()
    {
        GetReleasesOutMessage result = new() { Windows = _appReleasesProvider.WindowsRelease, Linux = _appReleasesProvider.LinuxRelease };

        return Ok(result);
    }
}

