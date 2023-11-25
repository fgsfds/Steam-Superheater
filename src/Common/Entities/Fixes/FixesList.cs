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
        required public int GameId { get; init; }

        /// <summary>
        /// Game title
        /// </summary>
        required public string GameName { get; init; }

        /// <summary>
        /// List of fixes
        /// </summary>
        required public List<BaseFixEntity> Fixes { get; init; }
    }
}
