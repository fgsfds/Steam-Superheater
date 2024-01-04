using Common.Entities.CombinedEntities;
using Common.Entities.Fixes.FileFix;

namespace Common.Providers
{
    public sealed class CombinedEntitiesProvider(
        FixesProvider fixesProvider,
        GamesProvider gamesProvider,
        InstalledFixesProvider installedFixesProvider
        )
    {
        private readonly FixesProvider _fixesProvider = fixesProvider;
        private readonly GamesProvider _gamesProvider = gamesProvider;
        private readonly InstalledFixesProvider _installedFixesProvider = installedFixesProvider;

        /// <summary>
        /// Get list of combined entities with fixes list being main entity
        /// </summary>
        public async Task<List<FixFirstCombinedEntity>> GetFixFirstEntitiesAsync(bool useCache)
        {
            var fixesLists = await _fixesProvider.GetListAsync(useCache);
            var sharedFixes = _fixesProvider.GetSharedFixes();
            var games = await _gamesProvider.GetListAsync(useCache);
            var installedFixes = await _installedFixesProvider.GetListAsync(useCache);

            List<FixFirstCombinedEntity> result = new(fixesLists.Count);

            foreach (var fixesList in fixesLists)
            {
                if (fixesList.GameId == 0)
                {
                    continue;
                }

                var game = games.FirstOrDefault(x => x.Id == fixesList.GameId);

                foreach (var fix in fixesList.Fixes)
                {
                    var installed = installedFixes.FirstOrDefault(x => x.GameId == fixesList.GameId && x.Guid == fix.Guid);

                    if (fix is FileFixEntity fileFix &&
                        fileFix.SharedFixGuid is not null)
                    {
                        var sharedFix = (FileFixEntity)sharedFixes.First(x => ((FileFixEntity)x).Guid == fileFix.SharedFixGuid);

                        sharedFix.InstallFolder = fileFix.SharedFixInstallFolder;

                        if (installed is FileInstalledFixEntity fileInstalled)
                        {
                            sharedFix.InstalledFix = fileInstalled.InstalledSharedFix;
                        }

                        fileFix.SharedFix = sharedFix;
                    }

                    if (installed is not null)
                    {
                        fix.InstalledFix = installed;
                    }
                }

                result.Add(new FixFirstCombinedEntity()
                { 
                    FixesList = fixesList,
                    Game = game
                });
            }

            result = [.. result.OrderByDescending(static x => x.IsGameInstalled)];

            return result;
        }
    }
}
