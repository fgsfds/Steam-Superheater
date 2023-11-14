using System.Xml.Serialization;

namespace Common.Entities.Fixes
{
    /// <summary>
    /// Entity containing game information and a list of fixes for it
    /// </summary>
    public sealed class FixesList
    {
        public FixesList(
            int gameId,
            string gameName,
            List<IFixEntity> fixes
            )
        {
            GameId = gameId;
            GameName = gameName;
            Fixes = fixes;
        }

        /// <summary>
        /// Steam ID of the game
        /// </summary>
        public int GameId { get; init; }

        /// <summary>
        /// Game title
        /// </summary>
        public string GameName { get; init; }

        /// <summary>
        /// List of fixes
        /// </summary>
        public List<IFixEntity> Fixes { get; init; }

        /// <summary>
        /// Serializer constructor
        /// </summary>
        private FixesList()
        {
        }
    }
}
