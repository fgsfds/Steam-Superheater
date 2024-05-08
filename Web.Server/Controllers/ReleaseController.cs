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
        private readonly AppReleasesProvider _fixesProvider;

        public ReleaseController(
            ILogger<ReleaseController> logger,
            AppReleasesProvider fixesProvider
            )
        {
            _logger = logger;
            _fixesProvider = fixesProvider;
        }

        [HttpGet("windows")]
        public AppUpdateEntity GetWindowsRelease() => _fixesProvider.WindowsRelease;

        [HttpGet("linux")]
        public AppUpdateEntity GetLinuxRelease() => _fixesProvider.LinuxRelease;
    }
}
