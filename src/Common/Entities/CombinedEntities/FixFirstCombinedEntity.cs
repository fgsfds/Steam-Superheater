using Common.Entities.Fixes;
using System.Diagnostics.CodeAnalysis;

namespace Common.Entities.CombinedEntities
{
    /// <summary>
    /// Object that combines fixes list with installed game
    /// </summary>
    public sealed class FixFirstCombinedEntity
    {
        [SetsRequiredMembers]
        public FixFirstCombinedEntity(
            FixesList fixesList,
            GameEntity? game,
            IEnumerable<BaseInstalledFixEntity>? installedFixes
            )
        {
            FixesList = fixesList;
            Game = game;

            if (installedFixes is null)
            {
                return;
            }

            foreach (var installedFix in installedFixes)
            {
                var guid = installedFix.Guid;

                var fix = FixesList.Fixes.FirstOrDefault(x => x.Guid == guid);

                if (fix is not null)
                {
                    fix.InstalledFix = installedFix;
                }
            }
        }

        /// <summary>
        /// List of fixes
        /// </summary>
        public required FixesList FixesList { get; init; }

        /// <summary>
        /// Game entity
        /// </summary>
        public required GameEntity? Game { get; init; }

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
        public int GameId => Game?.Id ?? FixesList.GameId;

        /// <summary>
        /// Does this game have installed fixes
        /// </summary>
        public bool HasInstalledFixes => FixesList.Fixes.Exists(static x => x.IsInstalled);

        /// <summary>
        /// Does this game have newer version of fixes
        /// </summary>
        public bool HasUpdates => FixesList.Fixes.Exists(static x => x.HasNewerVersion);

        public override string ToString() => GameName;
    }
}
