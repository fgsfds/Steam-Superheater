namespace Common.Entities
{
    public sealed class GameEntity
    {
        /// <summary>
        /// Steam game ID
        /// </summary>
        public required int Id { get; init; }

        /// <summary>
        /// Game title
        /// </summary>
        public required string Name { get; init; }

        /// <summary>
        /// Game install directory
        /// </summary>
        public required string InstallDir { get; init; }

        /// <summary>
        /// Game icon
        /// </summary>
        public string Icon => SteamTools.SteamInstallPath is null
            ? string.Empty
            : Path.Combine(
                SteamTools.SteamInstallPath,
                $"appcache{Path.DirectorySeparatorChar}librarycache{Path.DirectorySeparatorChar}{Id}_icon.jpg"
                );

        public override string ToString() => Name;
    }
}