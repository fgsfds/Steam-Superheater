using Microsoft.AspNetCore.Mvc;

namespace SuperheaterAPI.Controllers
{
    [ApiController]
    [Route("fixes")]
    public class FixesController : ControllerBase
    {
        private readonly ILogger<FixesController> _logger;
        private readonly HttpClient _httpClient;
        private readonly string _jsonUrl = "https://github.com/fgsfds/SteamFD-Fixes-Repo/raw/test/fixes.json";
        private string? _jsonContent;

        public FixesController(ILogger<FixesController> logger)
        {
            _logger = logger;
            _httpClient = new();
        }

        [HttpGet("{id:int?}")]
        public async Task<string> Get(int? id)
        {
            if (_jsonContent is null)
            {
                _jsonContent = await _httpClient.GetStringAsync(_jsonUrl);
            }

            return _jsonContent;
        }
    }
}
