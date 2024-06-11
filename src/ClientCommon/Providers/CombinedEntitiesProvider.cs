using Common;
using Common.Entities.CombinedEntities;
using Common.Entities.Fixes.FileFix;

namespace ClientCommon.Providers
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
        public async Task<Result<List<FixFirstCombinedEntity>?>> GetFixFirstEntitiesAsync()
        {
            var fixesLists = await _fixesProvider.GetFixesListAsync().ConfigureAwait(false);

            if (!fixesLists.IsSuccess)
            {
                return new(fixesLists.ResultEnum, null, fixesLists.Message);
            }

            var sharedFixes = _fixesProvider.GetSharedFixes();
            var games = await _gamesProvider.GetGamesListAsync().ConfigureAwait(false);
            var installedFixes = await _installedFixesProvider.GetInstalledFixesListAsync().ConfigureAwait(false);

            List<FixFirstCombinedEntity> result = new(fixesLists.ResultObject!.Count - 1);

            foreach (var fixesList in fixesLists.ResultObject)
            {
                if (fixesList.GameId == 0)
                {
                    continue;
                }

                foreach (var fix in fixesList.Fixes)
                {
                    var installed = installedFixes.FirstOrDefault(x => x.GameId == fixesList.GameId && x.Guid == fix.Guid);

                    if (fix is FileFixEntity fileFix &&
                        fileFix.SharedFixGuid is not null)
                    {
                        var sharedFix = sharedFixes.First(x => x.Guid == fileFix.SharedFixGuid).Clone();

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

                var game = games.FirstOrDefault(x => x.Id == fixesList.GameId);

                result.Add(new FixFirstCombinedEntity()
                { 
                    FixesList = fixesList,
                    Game = game
                });
            }

            result = [.. result.OrderByDescending(static x => x.IsGameInstalled)];

            return new(ResultEnum.Success, result, string.Empty);
        }
    }
}
