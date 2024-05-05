using Microsoft.AspNetCore.Mvc;

namespace SuperheaterAPI.Controllers
{
    [ApiController]
    [Route("")]
    public class DefaultController : ControllerBase
    {
        [HttpGet(Name = "GetDefault")]
        public string Get() => "Beep... boop...";
    }
}
