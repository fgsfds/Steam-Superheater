using Common.Entities.Fixes;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Immutable;
using System.Text.Json;

namespace SuperheaterAPI.Controllers
{
    [ApiController]
    [Route("api/fixes")]
    public class FixesController : ControllerBase
    {
        private readonly ILogger<FixesController> _logger;
        private readonly HttpClient _httpClient;
        private readonly string _jsonUrl = "https://github.com/fgsfds/SteamFD-Fixes-Repo/raw/test/fixes.json";
        private ImmutableList<FixesList>? _fixesList;

        public FixesController(ILogger<FixesController> logger)
        {
            _logger = logger;
            _httpClient = new();
        }

        [HttpGet("{id:int?}")]
        public async Task<ImmutableList<FixesList>> Get(int? id)
        {
            if (_fixesList is null)
            {
                await CreateFixesList();
            }

            return _fixesList!;
        }

        [HttpGet("gamescount")]
        public async Task<string> GetGamesCount()
        {
            if (_fixesList is null)
            {
                await CreateFixesList();
            }

            return _fixesList!.Count.ToString();
        }

        [HttpGet("fixescount")]
        public async Task<string> GetFixesCount()
        {
            if (_fixesList is null)
            {
                await CreateFixesList();
            }

            int count = 0;

            foreach (var fix in _fixesList!)
            {
                count += fix.Fixes.Count;
            }

            return count.ToString();
        }

        private async Task CreateFixesList()
        {
            var json = await _httpClient.GetStringAsync(_jsonUrl);
            var fixesList = JsonSerializer.Deserialize(json, FixesListContext.Default.ListFixesList);

            _fixesList = [.. fixesList];
        }
    }
}
