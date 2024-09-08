using Common.Entities;
using Microsoft.AspNetCore.Mvc;
using Web.Blazor.Providers;

namespace Web.Blazor.ControllersV1;

[Obsolete("Use V2 instead")]
[ApiController]
[Route("api/release")]
public sealed class ReleaseController : ControllerBase
{
    private readonly AppReleasesProvider _appReleasesProvider;


    public ReleaseController(AppReleasesProvider appReleasesProvider)
    {
        _appReleasesProvider = appReleasesProvider;
    }


    [Obsolete("Use V2 instead")]
    [HttpGet("windows")]
    public AppReleaseEntity? GetWindowsRelease() => _appReleasesProvider.WindowsRelease;


    [Obsolete("Use V2 instead")]
    [HttpGet("linux")]
    public AppReleaseEntity? GetLinuxRelease() => _appReleasesProvider.LinuxRelease;
}

