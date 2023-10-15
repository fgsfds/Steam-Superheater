using Common.CombinedEntities;
using Common.Entities;
using System.Collections.Immutable;
using System.Collections.ObjectModel;

namespace Common.Providers
{
    public sealed class CombinedEntitiesProvider
    {
        private readonly FixesProvider _fixesProvider;
        private readonly GamesProvider _gamesProvider;
        private readonly InstalledFixesProvider _installedFixesProvide;

        public CombinedEntitiesProvider(
            FixesProvider fixesProvider,
            GamesProvider gamesProvider,
            InstalledFixesProvider installedFixesProvider
            )
        {
            _fixesProvider = fixesProvider ?? throw new NullReferenceException(nameof(fixesProvider));
            _gamesProvider = gamesProvider ?? throw new NullReferenceException(nameof(gamesProvider));
            _installedFixesProvide = installedFixesProvider ?? throw new NullReferenceException(nameof(installedFixesProvider));
        }
        /// <summary>
        /// Get list of fix entities with installed fixes
        /// </summary>
        public async Task<List<FixesList>> GetFixesListAsync(bool useCache)
        {
            ImmutableList<FixesList> fixes;

            if (useCache)
            {
                fixes = await _fixesProvider.GetCachedListAsync();
            }
            else
            {
                fixes = await _fixesProvider.GetNewListAsync();
            }

            List<FixesList> result = new();

            foreach (var fix in fixes)
            {
                result.Add(new FixesList(fix.GameId, fix.GameName, new List<FixEntity>(fix.Fixes)));
            }

            return result;
        }

        /// <summary>
        /// Get list of combined entities with fixes list being main entity
        /// </summary>
        public async Task<List<FixFirstCombinedEntity>> GetFixFirstEntitiesAsync(bool useCache)
        {
            ImmutableList<FixesList> fixes;
            ImmutableList<GameEntity> games;
            ImmutableList<InstalledFixEntity> installed;

            if (useCache)
            {
                fixes = await _fixesProvider.GetCachedListAsync();
                games = await _gamesProvider.GetCachedListAsync();
            }
            else
            {
                fixes = await _fixesProvider.GetNewListAsync();
                games = await _gamesProvider.GetNewListAsync();
            }

            installed = _installedFixesProvide.GetInstalledFixes();

            List<FixFirstCombinedEntity> result = new();

            foreach (var fix in fixes)
            {
                var game = games.Where(x => x.Id == fix.GameId).FirstOrDefault();
                var inst = installed.Where(x => x.GameId == fix.GameId);

                result.Add(new FixFirstCombinedEntity(fix, game, inst));
            }

            result = result.OrderByDescending(x => x.IsGameInstalled).ToList();

            return result;
        }

        public static List<InstalledFixEntity> GetInstalledFixesFromCombined(List<FixFirstCombinedEntity> combinedList)
        {
            List<InstalledFixEntity> result = new();

            foreach (var combined in combinedList)
            {
                if (!combined.FixesList.Fixes.Any(x => x.IsInstalled))
                {
                    continue;
                }

                foreach (var fix in combined.FixesList.Fixes)
                {
                    if (fix.InstalledFix is null)
                    {
                        continue;
                    }

                    result.Add(fix.InstalledFix);
                }
            }

            return result;
        }
    }
}
