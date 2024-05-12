using Common.Entities.Fixes;
using Common.Messages;
using Microsoft.AspNetCore.Mvc;
using Superheater.Web.Server.Providers;
using System.Collections.Immutable;
using Web.Server.DbEntities;
using Web.Server.Helpers;

namespace Superheater.Web.Server.Controllers
{
    [ApiController]
    [Route("api/fixes")]
    public sealed class FixesController : ControllerBase
    {
        private readonly ILogger<FixesController> _logger;
        private readonly FixesProvider _fixesProvider;
        private readonly DatabaseContextFactory _dbContextFactory;
        private readonly SemaphoreSlim _semaphore;

        public FixesController(
            ILogger<FixesController> logger,
            FixesProvider fixesProvider,
            DatabaseContextFactory dbContextFactory
            )
        {
            _logger = logger;
            _fixesProvider = fixesProvider;
            _dbContextFactory = dbContextFactory;
            _semaphore = new(1);
        }

        [HttpGet]
        public ImmutableList<FixesList> GetFixesList() => _fixesProvider.FixesList;

        [HttpGet("{id:int?}")]
        public FixesList? GetFixById(int id) => _fixesProvider.FixesList.FirstOrDefault(x => x.GameId == id);

        [HttpGet("gamescount")]
        public int GetGamesCount() => _fixesProvider.GamesCount;

        [HttpGet("fixescount")]
        public int GetFixesCount() => _fixesProvider.FixesCount;

        [HttpGet("{guid:Guid}")]
        public bool GetFixById(Guid guid)
        {
            foreach (var fixesList in _fixesProvider.FixesList)
            {
                if (fixesList.Fixes.Any(x => x.Guid == guid))
                {
                    return true;
                }
            }

            return false;
        }

        [HttpGet("installs")]
        public Dictionary<Guid, int>? GetNumberOfInstalls()
        {
            using var dbContext = _dbContextFactory.Get();
            return dbContext.Installs.ToDictionary(x => x.FixGuid, x => x.Installs);
        }

        [HttpGet("installs/{guid:Guid}")]
        public int? GetNumberOfInstallsForFix(Guid guid)
        {
            using var dbContext = _dbContextFactory.Get();
            return dbContext.Installs.FirstOrDefault(x => x.FixGuid == guid)?.Installs;
        }

        [HttpPut("installs/add/{guid:Guid}")]
        public int? AddNumberOfInstalls(Guid guid)
        {
            _semaphore.Wait();

            using var dbContext = _dbContextFactory.Get();
            var row = dbContext.Installs.SingleOrDefault(b => b.FixGuid == guid);

            int installs;

            if (row is null)
            {
                InstallsEntity entity = new()
                {
                    FixGuid = guid,
                    Installs = 1
                };

                dbContext.Installs.Add(entity);
                installs = 1;
            }
            else
            {
                row.Installs += 1;
                installs = row.Installs;
            }

            dbContext.SaveChanges();

            _semaphore.Release();

            return installs;
        }

        [HttpGet("score/{guid:Guid}")]
        public int? GetRating(Guid guid)
        {
            using var dbContext = _dbContextFactory.Get();
            return dbContext.Scores.FirstOrDefault(x => x.FixGuid == guid)?.Rating;
        }

        [HttpPut("score/change")]
        public int? ChangeRating([FromBody] RatingMessage message)
        {
            _semaphore.Wait();

            using var dbContext = _dbContextFactory.Get();
            var row = dbContext.Scores.SingleOrDefault(b => b.FixGuid == message.FixGuid);

            int rating;

            if (row is null)
            {
                ScoresEntity entity = new()
                {
                    FixGuid = message.FixGuid,
                    Rating = message.Score
                };

                dbContext.Scores.Add(entity);
                rating = message.Score;
            }
            else
            {
                row.Rating += message.Score;
                rating = row.Rating;
            }

            dbContext.SaveChanges();

            _semaphore.Release();

            return rating;
        }

        [HttpPost("report")]
        public void ReportFix([FromBody] ReportMessage message)
        {
            using var dbContext = _dbContextFactory.Get();

            ReportsEntity entity = new()
            {
                FixGuid = message.FixGuid,
                ReportText = message.ReportText
            };

            dbContext.Reports.Add(entity);
            dbContext.SaveChanges();
        }
    }
}
