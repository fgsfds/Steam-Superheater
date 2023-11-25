namespace Common.Entities
{
    public sealed class GameEntity
    {
        /// <summary>
        /// Steam game ID
        /// </summary>
        required public int Id { get; init; }

        /// <summary>
        /// Game title
        /// </summary>
        required public string Name { get; init; }

        /// <summary>
        /// Game install directory
        /// </summary>
        required public string InstallDir { get; set; }

        /// <summary>
        /// Game icon
        /// </summary>
        public string Icon => SteamTools.SteamInstallPath is null
            ? string.Empty
            : Path.Combine(
                SteamTools.SteamInstallPath,
                @$"appcache{Path.DirectorySeparatorChar}librarycache{Path.DirectorySeparatorChar}{Id}_icon.jpg"
                );

        public override string ToString() => Name;
    }
}