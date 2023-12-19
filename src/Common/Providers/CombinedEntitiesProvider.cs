using Common.Entities.CombinedEntities;
using Common.Entities.Fixes;

namespace Common.Providers
{
    public sealed class CombinedEntitiesProvider(
        FixesProvider _fixesProvider,
        GamesProvider _gamesProvider,
        InstalledFixesProvider _installedFixesProvider
        )
    {
        /// <summary>
        /// Get list of combined entities with fixes list being main entity
        /// </summary>
        public async Task<List<FixFirstCombinedEntity>> GetFixFirstEntitiesAsync(bool useCache)
        {
            var fixes = await _fixesProvider.GetListAsync(useCache);
            var games = await _gamesProvider.GetListAsync(useCache);
            var installed = await _installedFixesProvider.GetListAsync(useCache);

            List<FixFirstCombinedEntity> result = new(fixes.Count);

            foreach (var fix in fixes)
            {
                var id = fix.GameId;

                var game = games.FirstOrDefault(x => x.Id == id);
                var installedForGame = installed.Where(x => x.GameId == id);

                result.Add(new FixFirstCombinedEntity(fix, game, installedForGame));
            }

            result = [.. result.OrderByDescending(static x => x.IsGameInstalled)];

            return result;
        }

        /// <summary>
        /// Get list of installed fixes from the list of combined entities
        /// </summary>
        /// <param name="combinedList">List of combined entities</param>
        /// <returns>List of installed fixes</returns>
        public static List<BaseInstalledFixEntity> GetInstalledFixesFromCombined(List<FixFirstCombinedEntity> combinedList)
        {
            List<BaseInstalledFixEntity> result = [];

            foreach (var combined in combinedList)
            {
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
