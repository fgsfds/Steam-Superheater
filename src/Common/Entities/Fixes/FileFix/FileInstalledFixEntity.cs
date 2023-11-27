namespace Common.Entities.Fixes.FileFix
{
    public sealed class FileInstalledFixEntity() : BaseInstalledFixEntity
    {
        /// <summary>
        /// Name of the backup folder
        /// </summary>
        public required string? BackupFolder { get; init; }

        /// <summary>
        /// Paths to files relative to the game folder
        /// </summary>
        public required List<string>? FilesList { get; init; }
    }
}
