using Common.Entities;
using Microsoft.AspNetCore.Mvc;
using Web.Blazor.Providers;

namespace Web.Blazor.ControllersV2;

[ApiController]
[Route("api2/release")]
public sealed class ReleaseController : ControllerBase
{
    private readonly AppReleasesProvider _appReleasesProvider;


    public ReleaseController(AppReleasesProvider appReleasesProvider)
    {
        _appReleasesProvider = appReleasesProvider;
    }


    [HttpGet("windows")]
    [ProducesResponseType(typeof(AppReleaseEntity), StatusCodes.Status200OK)]
    public ActionResult<AppReleaseEntity> GetWindowsRelease()
    {
        return Ok(_appReleasesProvider.WindowsRelease);
    }

    [HttpGet("linux")]
    [ProducesResponseType(typeof(AppReleaseEntity), StatusCodes.Status200OK)]
    public ActionResult<AppReleaseEntity> GetLinuxRelease()
    {
        return Ok(_appReleasesProvider.LinuxRelease);
    }
}

