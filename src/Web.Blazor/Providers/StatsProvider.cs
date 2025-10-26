using Database.Server;
using Database.Server.DbEntities;
using Microsoft.EntityFrameworkCore;

namespace Web.Blazor.Providers;

public sealed class StatsProvider
{
    private readonly ILogger<StatsProvider> _logger;
    private readonly DatabaseContextFactory _dbContextFactory;

    public FixesStats? Stats { get; private set; }


    public StatsProvider(
        DatabaseContextFactory dbContextFactory,
        ILogger<StatsProvider> logger
        )
    {
        _dbContextFactory = dbContextFactory;
        _logger = logger;
    }


    public void UpdateStats()
    {
        _logger.LogInformation("Started creating stats");

        using var dbContext = _dbContextFactory.Get();

        var games = dbContext.Games.AsNoTracking().OrderBy(x => x.Name).Where(x => x.Id != 0).ToDictionary(x => x.Id, x => x.Name);

        List<FixesDbEntity> fixes = [];
        List<string> noIntro = [];

        foreach (var fix in dbContext.Fixes.AsNoTracking().Where(x => !x.IsDisabled))
        {
            if (fix.Name.StartsWith("No Intro Fix", StringComparison.OrdinalIgnoreCase))
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
        fixesStats.NoIntroFixes = [.. noIntro.Order()];

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

