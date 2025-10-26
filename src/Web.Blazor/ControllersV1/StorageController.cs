using System.Web;
using Microsoft.AspNetCore.Mvc;
using Web.Blazor.Helpers;

namespace Web.Blazor.ControllersV1;

[Obsolete("Remove when there's no versions <2.0.0")]
[ApiController]
[Route("api/storage")]
public sealed class StorageController : ControllerBase
{
    private readonly S3Client _s3controller;


    public StorageController(S3Client s3controller)
    {
        _s3controller = s3controller;
    }


    [Obsolete("Remove when there's no versions <2.0.0")]
    [HttpGet("url/{path}")]
    public string GetSignedUrl(string path)
    {
        var filePath = HttpUtility.UrlDecode(path);

        var signedUrl = _s3controller.GetSignedUrl(filePath);

        return signedUrl!;
    }
}

