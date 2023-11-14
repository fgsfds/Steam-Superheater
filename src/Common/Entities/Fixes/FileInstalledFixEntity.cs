namespace Common.Entities.Fixes
{
    public sealed class FileInstalledFixEntity : IInstalledFixEntity
    {
        public FileInstalledFixEntity(int id, Guid guid, int version, string backupFolder, List<string>? list)
        {
            GameId = id;
            Guid = guid;
            Version = version;
            BackupFolder = backupFolder;
            FilesList = list;
        }

        /// <summary>
        /// Steam game ID
        /// </summary>
        public int GameId { get; init; }

        /// <summary>
        /// Fix GUID
        /// </summary>
        public Guid Guid { get; init; }

        /// <summary>
        /// Installed version
        /// </summary>
        public int Version { get; init; }

        /// <summary>
        /// Name of the backup folder
        /// </summary>
        public string BackupFolder { get; init; }

        /// <summary>
        /// Paths to files relative to the game folder
        /// </summary>
        public List<string>? FilesList { get; init; }

        /// <summary>
        /// Serializer constructor
        /// </summary>
        private FileInstalledFixEntity()
        {
        }
    }
}
