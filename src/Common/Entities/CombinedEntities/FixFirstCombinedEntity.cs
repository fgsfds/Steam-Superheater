using Common.Entities;
using Common.Entities.Fixes;

namespace Common.CombinedEntities
{
    /// <summary>
    /// Object that combines fixes list with installed game
    /// </summary>
    public sealed class FixFirstCombinedEntity
    {
        public FixFirstCombinedEntity(
            FixesList fixesList,
            GameEntity? game,
            IEnumerable<BaseInstalledFixEntity>? installedFixes
            )
        {
            FixesList = fixesList;
            Game = game;

            if (installedFixes is not null)
            {
                foreach (var installedFix in installedFixes)
                {
                    var fix = FixesList.Fixes.Where(x => x.Guid == installedFix.Guid).FirstOrDefault();

                    if (fix is not null)
                    {
                        fix.InstalledFix = installedFix;
                    }
                }
            }
        }

        /// <summary>
        /// List of fixes
        /// </summary>
        public FixesList FixesList { get; set; }

        /// <summary>
        /// Game entity
        /// </summary>
        public GameEntity? Game { get; init; }

        /// <summary>
        /// Is game installed
        /// </summary>
        public bool IsGameInstalled => Game is not null;

        /// <summary>
        /// Name of the game
        /// </summary>
        public string GameName => Game is not null ? Game.Name : FixesList.GameName;

        /// <summary>
        /// Id of the game
        /// </summary>
        public int GameId => Game is not null ? Game.Id : FixesList.GameId;

        /// <summary>
        /// Does this game have installed fixes
        /// </summary>
        public bool HasInstalledFixes => FixesList.Fixes.Any(x => x.IsInstalled);

        /// <summary>
        /// Does this game have newer version of fixes
        /// </summary>
        public bool HasUpdates => FixesList.Fixes.Any(x => x.HasNewerVersion);

        public override string ToString() => GameName;
    }
}
