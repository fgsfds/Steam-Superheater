using Common.Entities;
using Microsoft.AspNetCore.Mvc;
using Superheater.Web.Server.Providers;
using System.Collections.Immutable;

namespace Superheater.Web.Server.Controllers
{
    [ApiController]
    [Route("api/news")]
    public sealed class NewsController : ControllerBase
    {
        private readonly ILogger<NewsController> _logger;
        private readonly NewsProvider _fixesProvider;

        public NewsController(
            ILogger<NewsController> logger,
            NewsProvider fixesProvider
            )
        {
            _logger = logger;
            _fixesProvider = fixesProvider;
        }

        [HttpGet]
        public ImmutableList<NewsEntity> GetFixesList() => _fixesProvider.NewsList;
    }
}
