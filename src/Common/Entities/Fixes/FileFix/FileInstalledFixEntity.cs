namespace Common.Entities.Fixes.FileFix
{
    public sealed class FileInstalledFixEntity() : BaseInstalledFixEntity
    {
        /// <summary>
        /// Name of the backup folder
        /// </summary>
        required public string? BackupFolder { get; init; }

        /// <summary>
        /// Paths to files relative to the game folder
        /// </summary>
        required public List<string>? FilesList { get; init; }
    }
}
