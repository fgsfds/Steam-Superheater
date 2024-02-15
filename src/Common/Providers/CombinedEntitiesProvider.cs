using Common.Entities.CombinedEntities;
using Common.Entities.Fixes.FileFix;
using Common.Providers.Cached;

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
        public async Task<Dictionary<string, FixFirstCombinedEntity>> GetFixFirstEntitiesAsync(bool useCache)
        {
            var fixesLists = await _fixesProvider.GetListAsync(useCache);
            var sharedFixes = _fixesProvider.GetSharedFixes();
            var games = await _gamesProvider.GetListAsync(useCache);
            var installedFixes = await _installedFixesProvider.GetListAsync(useCache);

            Dictionary<string, FixFirstCombinedEntity> result = new(fixesLists.Count);

            foreach (var fixesList in fixesLists)
            {
                if (fixesList.Key == 0)
                {
                    continue;
                }

                foreach (var fix in fixesList.Value.Fixes)
                {
                    var installed = installedFixes[fix.Key];

                    if (fix.Value is FileFixEntity fileFix &&
                        fileFix.SharedFixGuid is not null)
                    {
                        var sharedFix = ((FileFixEntity)sharedFixes[(Guid)fileFix.SharedFixGuid]).Clone();

                        sharedFix.InstallFolder = fileFix.SharedFixInstallFolder;

                        if (installed is FileInstalledFixEntity fileInstalled)
                        {
                            sharedFix.InstalledFix = fileInstalled.InstalledSharedFix;
                        }

                        fileFix.SharedFix = sharedFix;
                    }

                    if (installed is not null)
                    {
                        fix.Value.InstalledFix = installed;
                    }
                }

                var game = games[fixesList.Key];

                result.Add(
                    fixesList.Value.GameName,
                        new FixFirstCombinedEntity()
                    { 
                        FixesList = fixesList.Value,
                        Game = game
                    });
            }

            //result = [.. result.OrderByDescending(static x => x.IsGameInstalled)];

            return result;
        }
    }
}
