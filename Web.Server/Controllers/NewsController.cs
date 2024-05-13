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
        private readonly NewsProvider _newsProvider;

        public NewsController(
            ILogger<NewsController> logger,
            NewsProvider newsProvider
            )
        {
            _logger = logger;
            _newsProvider = newsProvider;
        }

        [HttpGet]
        public ImmutableList<NewsEntity> GetNewsList() => _newsProvider.GetNews();

        [HttpPost("add")]
        public StatusCodeResult AddNews([FromBody] Tuple<DateTime, string, string> message)
        {
            var result = _newsProvider.AddNews(message);

            if (result)
            {
                return StatusCode(StatusCodes.Status200OK);
            }
            else
            {
                return StatusCode(StatusCodes.Status403Forbidden);
            }
        }

        [HttpPut("change")]
        public StatusCodeResult ChangeNews([FromBody] Tuple<DateTime, string, string> message)
        {
            var result = _newsProvider.ChangeNews(message);

            if (result)
            {
                return StatusCode(StatusCodes.Status200OK);
            }
            else
            {
                return StatusCode(StatusCodes.Status403Forbidden);
            }
        }

        //Expected format 2020-09-22T18:07:13
        //[HttpGet("{date:DateTime?}")]
        //public ImmutableList<NewsEntity>? GetNewsByDate(DateTime date) => [.. _newsProvider.NewsList.Where(x => x.Date > date)];
    }
}
