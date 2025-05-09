namespace Common.Entities.Fixes.FileFix;

public sealed class FileInstalledFixEntity : BaseInstalledFixEntity
{
    /// <summary>
    /// Name of the backup folder
    /// </summary>
    public string? BackupFolder { get; init; }

    /// <summary>
    /// Paths to files relative to the game folder
    /// </summary>
    [Obsolete("Replaced with Hashes")]
    public List<string>? FilesList { get; init; }

    public FileInstalledFixEntity? InstalledSharedFix { get; init; }

    public List<string>? WineDllOverrides { get; init; }

    public Dictionary<string, long?>? Hashes { get; init; }
}
