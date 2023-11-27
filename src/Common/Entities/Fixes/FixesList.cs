namespace Common.Entities.Fixes
{
    /// <summary>
    /// Entity containing game information and a list of fixes for it
    /// </summary>
    public sealed class FixesList
    {
        /// <summary>
        /// Steam ID of the game
        /// </summary>
        public required int GameId { get; init; }

        /// <summary>
        /// Game title
        /// </summary>
        public required string GameName { get; init; }

        /// <summary>
        /// List of fixes
        /// </summary>
        public required List<BaseFixEntity> Fixes { get; init; }
    }
}
