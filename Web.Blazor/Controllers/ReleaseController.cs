using Common.Entities;
using Microsoft.AspNetCore.Mvc;
using Web.Blazor.Providers;

namespace Web.Blazor.Controllers;

[ApiController]
[Route("api/release")]
public sealed class ReleaseController : ControllerBase
{
    private readonly AppReleasesProvider _appReleasesProvider;

    public ReleaseController(
        AppReleasesProvider appReleasesProvider
        )
    {
        _appReleasesProvider = appReleasesProvider;
    }

    [HttpGet("windows")]
    public AppReleaseEntity? GetWindowsRelease() => _appReleasesProvider.WindowsRelease;

    [HttpGet("linux")]
    public AppReleaseEntity? GetLinuxRelease() => _appReleasesProvider.LinuxRelease;
}

