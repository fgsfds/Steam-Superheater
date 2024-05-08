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
        public ImmutableList<NewsEntity> GetNewsList() => _fixesProvider.NewsList;

        //Expected format 2020-09-22T18:07:13
        [HttpGet("{date:DateTime?}")]
        public ImmutableList<NewsEntity>? GetNewsByDate(DateTime date) => [.. _fixesProvider.NewsList.Where(x => x.Date > date)];
    }
}
