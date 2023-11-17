namespace Common.Entities.Fixes.FileFix
{
    public sealed class FileInstalledFixEntity() : BaseInstalledFixEntity
    {
        public FileInstalledFixEntity(int id, Guid guid, int version, string backupFolder, List<string>? list) : this()
        {
            GameId = id;
            Guid = guid;
            Version = version;
            BackupFolder = backupFolder;
            FilesList = list;
        }

        /// <summary>
        /// Name of the backup folder
        /// </summary>
        public string BackupFolder { get; init; }

        /// <summary>
        /// Paths to files relative to the game folder
        /// </summary>
        public List<string>? FilesList { get; init; }
    }
}
