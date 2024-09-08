using Microsoft.AspNetCore.Mvc;
using System.Web;
using Web.Blazor.Helpers;
using Microsoft.AspNetCore.Http;

namespace Web.Blazor.ControllersV2;

[ApiController]
[Route("api2/storage")]
public sealed class StorageController : ControllerBase
{
    private readonly S3Client _s3controller;

    public StorageController(S3Client s3controller)
    {
        _s3controller = s3controller;
    }


    [HttpGet("url/{path}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public ActionResult<string> GetSignedUrl(string path)
    {
        var filePath = HttpUtility.UrlDecode(path);

        if (string.IsNullOrWhiteSpace(filePath))
        {
            return BadRequest();
        }

        var signedUrl = _s3controller.GetSignedUrl(filePath);

        if (string.IsNullOrWhiteSpace(signedUrl))
        {
            return StatusCode(500);
        }

        return Ok(signedUrl);
    }
}

