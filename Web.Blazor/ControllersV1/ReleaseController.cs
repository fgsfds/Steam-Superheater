using Common.Entities;
using Microsoft.AspNetCore.Mvc;
using Web.Blazor.Providers;

namespace Web.Blazor.ControllersV1;

[Obsolete("Remove when there's no versions <2.0.0")]
[ApiController]
[Route("api/release")]
public sealed class ReleaseController : ControllerBase
{
    private readonly AppReleasesProvider _appReleasesProvider;


    public ReleaseController(AppReleasesProvider appReleasesProvider)
    {
        _appReleasesProvider = appReleasesProvider;
    }


    [Obsolete("Remove when there's no versions <2.0.0")]
    [HttpGet("windows")]
    public AppReleaseEntity? GetWindowsRelease() => _appReleasesProvider.WindowsRelease;


    [Obsolete("Remove when there's no versions <2.0.0")]
    [HttpGet("linux")]
    public AppReleaseEntity? GetLinuxRelease() => _appReleasesProvider.LinuxRelease;
}

