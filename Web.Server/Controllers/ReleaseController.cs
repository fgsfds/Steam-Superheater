using Common.Entities;
using Microsoft.AspNetCore.Mvc;
using Superheater.Web.Server.Providers;

namespace Superheater.Web.Server.Controllers
{
    [ApiController]
    [Route("api/release")]
    public sealed class ReleaseController : ControllerBase
    {
        private readonly ILogger<ReleaseController> _logger;
        private readonly AppReleasesProvider _appReleasesProvider;

        public ReleaseController(
            ILogger<ReleaseController> logger,
            AppReleasesProvider appReleasesProvider
            )
        {
            _logger = logger;
            _appReleasesProvider = appReleasesProvider;
        }

        [HttpGet("windows")]
        public AppReleaseEntity? GetWindowsRelease() => _appReleasesProvider.WindowsRelease;

        [HttpGet("linux")]
        public AppReleaseEntity? GetLinuxRelease() => _appReleasesProvider.LinuxRelease;
    }
}
