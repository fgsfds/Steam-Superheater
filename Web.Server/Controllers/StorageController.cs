using Microsoft.AspNetCore.Mvc;
using System.Web;
using Web.Server.Helpers;

namespace Superheater.Web.Server.Controllers
{
    [ApiController]
    [Route("api/storage")]
    public sealed class StorageController : ControllerBase
    {
        private readonly ILogger<StorageController> _logger;
        private readonly S3Client _s3controller;

        public StorageController(
            ILogger<StorageController> logger,
            S3Client s3controller
            )
        {
            _logger = logger;
            _s3controller = s3controller;
        }

        [HttpGet("url/{path}")]
        public string GetSignedUrl(string path)
        {
            var filePath = HttpUtility.UrlDecode(path);

            var signedUrl = _s3controller.GetSignedUrl(filePath);

            return signedUrl;
        }
    }
}
