using SteamFDTCommon.Entities;
using System.Collections.ObjectModel;

namespace SteamFDCommon.CombinedEntities
{
    /// <summary>
    /// Object that combines game and its fixes
    /// </summary>
    public class GameFirstCombinedEntity
    {
        /// <summary>
        /// Game entity
        /// </summary>
        public GameEntity Game { get; init; }

        /// <summary>
        /// List of fixes
        /// </summary>
        public List<FixEntity>? Fixes { get; set; }

        public override string ToString() => Game.Name;

        public GameFirstCombinedEntity(
            GameEntity game,
            ObservableCollection<FixEntity>? fixes,
            List<InstalledFixEntity>? installedFixes
            )
        {
            Game = game;

            if (fixes is null)
            {
                Fixes = new();
            }

            if (fixes is not null)
            {
                Fixes = new List<FixEntity>(fixes);
            }

            if (Fixes is not null &&
                installedFixes is not null)
            {
                foreach (var installedFix in installedFixes)
                {
                    var fix = Fixes.Where(x => x.Guid == installedFix.Guid).FirstOrDefault();

                    if (fix is not null)
                    {
                        fix.InstalledFix = installedFix;
                    }
                }
            }
        }
    }
}
