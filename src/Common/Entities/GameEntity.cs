namespace Common.Entities
{
    public sealed class GameEntity(
        int id,
        string name,
        string dir
        )
    {

        /// <summary>
        /// Steam game ID
        /// </summary>
        public int Id { get; init; } = id;

        /// <summary>
        /// Game title
        /// </summary>
        public string Name { get; init; } = name;

        /// <summary>
        /// Game install directory
        /// </summary>
        public string InstallDir { get; set; } = dir;

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