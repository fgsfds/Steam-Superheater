using SteamFDCommon.CombinedEntities;
using SteamFDCommon.DI;
using SteamFDCommon.Entities;
using System.Collections.ObjectModel;

namespace SteamFDCommon.Providers
{
    public static class CombinedEntitiesProvider
    {
        //public static async Task<List<GameFirstCombinedEntity>> GetGameFirstEntitiesListAsync(bool useCache)
        //{
        //    List<GameEntity> games;
        //    List<FixesList> fixes;
        //    List<InstalledFixEntity> installed;

        //    if (useCache)
        //    {
        //        games = BindingsManager.Instance.GetInstance<GamesProvider>().GetCachedGamesList();
        //        fixes = await BindingsManager.Instance.GetInstance<FixesProvider>().GetCachedFixesListAsync();
        //        installed = BindingsManager.Instance.GetInstance<InstalledFixesProvider>().GetCachedInstalledFixesList();
        //    }
        //    else
        //    {
        //        games = BindingsManager.Instance.GetInstance<GamesProvider>().GetNewGamesList();
        //        fixes = await BindingsManager.Instance.GetInstance<FixesProvider>().GetNewFixesListAsync();
        //        installed = BindingsManager.Instance.GetInstance<InstalledFixesProvider>().GetNewInstalledFixesList();
        //    }

        //    List<GameFirstCombinedEntity> result = new();

        //    foreach (var game in games)
        //    {
        //        var fixesList = fixes.Where(x => x.GameId == game.Id).FirstOrDefault()?.Fixes;

        //        if (fixesList is null)
        //        {
        //            continue;
        //        }

        //        var installedFix = installed.Where(f => f.GameId == game.Id).ToList();

        //        result.Add(
        //            new GameFirstCombinedEntity(
        //                game,
        //                fixesList,
        //                installedFix
        //                )
        //            );
        //    }

        //    return result;
        //}

        /// <summary>
        /// Get list of entities
        /// </summary>
        /// <returns></returns>
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
        /// Get list of entities
        /// </summary>
        /// <returns></returns>
        public static async Task<List<FixFirstCombinedEntity>> GetFixFirstEntitiesAsync1(bool useCache)
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
                if (fix.GameName.Equals("!Software"))
                {
                    continue;
                }

                var game = games.Where(x => x.Id == fix.GameId).FirstOrDefault();
                var inst = installed.Where(x => x.GameId == fix.GameId);

                result.Add(new FixFirstCombinedEntity(fix, game, inst));
            }

            result = result.OrderByDescending(x => x.IsGameInstalled).ToList();

            return result;
        }

        public static List<InstalledFixEntity> GetInstalledFixesFromCombined(List<FixFirstCombinedEntity> combinedList)
        {
            return new();

            //List<InstalledFixEntity> result = new();

            //foreach (var combined in combinedList)
            //{
            //    if (combined.Fixes is null ||
            //        !combined.Fixes.Any(x => x.IsInstalled))
            //    {
            //        continue;
            //    }

            //    foreach (var fix in combined.Fixes)
            //    {
            //        if (fix.InstalledFix is null)
            //        {
            //            continue;
            //        }

            //        result.Add(fix.InstalledFix);
            //    }
            //}

            //return result;
        }
    }
}
