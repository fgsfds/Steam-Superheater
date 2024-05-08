using Common.Entities.Fixes;
using Microsoft.AspNetCore.Mvc;
using Superheater.Web.Server.Providers;
using System.Collections.Immutable;

namespace Superheater.Web.Server.Controllers
{
    [ApiController]
    [Route("api/fixes")]
    public sealed class FixesController : ControllerBase
    {
        private readonly ILogger<FixesController> _logger;
        private readonly FixesProvider _fixesProvider;

        public FixesController(
            ILogger<FixesController> logger,
            FixesProvider fixesProvider
            )
        {
            _logger = logger;
            _fixesProvider = fixesProvider;
        }

        [HttpGet]
        public ImmutableList<FixesList> GetFixesList() => _fixesProvider.FixesList;

        [HttpGet("{id:int?}")]
        public FixesList? GetFixById(int id) => _fixesProvider.FixesList.FirstOrDefault(x => x.GameId == id);

        [HttpGet("gamescount")]
        public int GetGamesCount() => _fixesProvider.GamesCount;

        [HttpGet("fixescount")]
        public int GetFixesCount() => _fixesProvider.FixesCount;
    }
}
