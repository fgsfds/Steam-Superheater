using Microsoft.EntityFrameworkCore;
using Web.Server.DbEntities;
using Web.Server.Helpers;

namespace Superheater.Web.Server.Providers
{
    public sealed class StatsProvider
    {
        private readonly ILogger<NewsProvider> _logger;
        private readonly DatabaseContextFactory _dbContextFactory;

        public FixesStats? Stats {get; private set;}


        public StatsProvider(
            DatabaseContextFactory dbContextFactory,
            ILogger<NewsProvider> logger
            )
        {
            _dbContextFactory = dbContextFactory;
            _logger = logger;
        }


        public void CreateStats()
        {
            _logger.LogInformation("Started creating stats");

            using var dbContext = _dbContextFactory.Get();

            var games = dbContext.Games.AsNoTracking().OrderBy(x => x.Name).Where(x => x.Id != 0).ToDictionary(x => x.Id, x => x.Name);

            List<FixesDbEntity> fixes = [];
            List<string> noIntro = [];

            foreach (var fix in dbContext.Fixes.AsNoTracking().Where(x => !x.IsDisabled))
            {
                if (fix.Name.Equals("No Intro Fix", StringComparison.InvariantCultureIgnoreCase))
                {
                    var gameName = games[fix.GameId];
                    noIntro.Add(gameName);
                }
                else
                {
                    fixes.Add(fix);
                }
            }

            var gamesCount = 0;
            var fixesCount = 0;

            var fixesStats = new FixesStats();

            var fixesLookup = fixes.ToLookup(x => x.GameId);

            foreach (var game in games)
            {
                var fixesList = fixesLookup[game.Key].Select(x => x.Name);

                if (!fixesList.Any())
                {
                    continue;
                }

                fixesStats.FixesLists.Add(new FixesLists
                {
                    Game = game.Value,
                    Fixes = [.. fixesList]
                });

                gamesCount++;
                fixesCount += fixesList.Count();
            }

            fixesStats.GamesCount = gamesCount;
            fixesStats.FixesCount = fixesCount;
            fixesStats.NoIntroFixes = [.. noIntro.OrderBy(x => x)];

            Stats = fixesStats;

            _logger.LogInformation("Finished creating stats");
        }


        public sealed class FixesStats
        {
            public int GamesCount { get; set; }
            public int FixesCount { get; set; }
            public List<FixesLists> FixesLists { get; set; } = [];
            public List<string> NoIntroFixes { get; set; } = [];
        }

        public sealed class FixesLists
        {
            public string Game { get; set; } = string.Empty;
            public List<string> Fixes { get; set; } = [];
        }
    }
}
