using SteamFDCommon.CombinedEntities;
using SteamFDCommon.DI;
using SteamFDCommon.Entities;
using System.Collections.ObjectModel;

namespace SteamFDCommon.Providers
{
    public static class CombinedEntitiesProvider
    {
        /// <summary>
        /// Get list of fix entities with installed fixes
        /// </summary>
        public static async Task<List<FixesList>> GetFixesListAsync(bool useCache)
        {
            List<FixesList> fixes;

            if (useCache)
            {
                fixes = await BindingsManager.Instance.GetInstance<FixesProvider>().GetCachedFixesListAsync();
            }
            else
            {
                fixes = await BindingsManager.Instance.GetInstance<FixesProvider>().GetNewFixesListAsync();
            }


            List<FixesList> result = new();

            foreach (var fix in fixes)
            {
                result.Add(new FixesList(fix.GameId, fix.GameName, new ObservableCollection<FixEntity>(fix.Fixes)));
            }

            return result;
        }

        /// <summary>
        /// Get list of combined entities with fixes list being main entity
        /// </summary>
        public static async Task<List<FixFirstCombinedEntity>> GetFixFirstEntitiesAsync(bool useCache)
        {
            List<FixesList> fixes;
            List<GameEntity> games;
            List<InstalledFixEntity> installed;

            if (useCache)
            {
                fixes = await BindingsManager.Instance.GetInstance<FixesProvider>().GetCachedFixesListAsync();
                games = BindingsManager.Instance.GetInstance<GamesProvider>().GetCachedGamesList();
                installed = BindingsManager.Instance.GetInstance<InstalledFixesProvider>().GetCachedInstalledFixesList();
            }
            else
            {
                fixes = await BindingsManager.Instance.GetInstance<FixesProvider>().GetNewFixesListAsync();
                games = BindingsManager.Instance.GetInstance<GamesProvider>().GetNewGamesList();
                installed = BindingsManager.Instance.GetInstance<InstalledFixesProvider>().GetNewInstalledFixesList();
            }

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
