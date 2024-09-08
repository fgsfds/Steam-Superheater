using Api.Common.Messages;
using Common.Entities;
using Common.Enums;
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
        Dictionary<OSEnum, AppReleaseEntity> releases = [];
        releases.Add(OSEnum.Windows, _appReleasesProvider.WindowsRelease);
        releases.Add(OSEnum.Linux, _appReleasesProvider.LinuxRelease);

        GetReleasesOutMessage result = new() { Releases = releases };

        return Ok(result);
    }
}

