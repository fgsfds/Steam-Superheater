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

        public FixesController(
            ILogger<FixesController> logger,
            FixesProvider fixesProvider,
            DatabaseContextFactory dbContextFactory
            )
        {
            _logger = logger;
            _fixesProvider = fixesProvider;
            _dbContextFactory = dbContextFactory;
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
            return dbContext.Downloads.ToDictionary(x => x.FixGuid, x => x.Installs);
        }

        [HttpGet("installs/{guid:Guid}")]
        public int? GetNumberOfInstallsForFix(Guid guid)
        {
            using var dbContext = _dbContextFactory.Get();
            return dbContext.Downloads.FirstOrDefault(x => x.FixGuid == guid)?.Installs;
        }

        [HttpPost("installs/add/{guid:Guid}")]
        public async Task<int?> AddNumberOfInstallsAsync(Guid guid)
        {
            using var dbContext = _dbContextFactory.Get();
            var row = dbContext.Downloads.SingleOrDefault(b => b.FixGuid == guid);

            int installs;

            if (row is null)
            {
                InstallsEntity entity = new()
                {
                    FixGuid = guid,
                    Installs = 1
                };

                dbContext.Downloads.Add(entity);
                installs = 1;
            }
            else
            {
                row.Installs += 1;
                installs = row.Installs;
            }

            await dbContext.SaveChangesAsync().ConfigureAwait(false);

            return installs;
        }

        [HttpGet("score/{guid:Guid}")]
        public int? GetRating(Guid guid)
        {
            using var dbContext = _dbContextFactory.Get();
            return dbContext.Rating.FirstOrDefault(x => x.FixGuid == guid)?.Rating;
        }

        [HttpPost("score/change")]
        public async Task<int?> ChangeRatingAsync([FromBody] RatingMessage message)
        {
            using var dbContext = _dbContextFactory.Get();
            var row = dbContext.Rating.SingleOrDefault(b => b.FixGuid == message.FixGuid);

            int rating;

            if (row is null)
            {
                ScoreEntity entity = new()
                {
                    FixGuid = message.FixGuid,
                    Rating = message.Score
                };

                dbContext.Rating.Add(entity);
                rating = message.Score;
            }
            else
            {
                row.Rating += message.Score;
                rating = row.Rating;
            }

            await dbContext.SaveChangesAsync().ConfigureAwait(false);

            return rating;
        }
    }
}
