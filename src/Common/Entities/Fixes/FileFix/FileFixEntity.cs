using Common.Enums;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Common.Entities.Fixes.FileFix
{
    /// <summary>
    /// File fix entity
    /// </summary>
    public sealed class FileFixEntity : BaseFixEntity
    {
        [SetsRequiredMembers]
        public FileFixEntity()
        {
            Name = string.Empty;
            Version = 1;
            Guid = Guid.NewGuid();
            Description = null;
            Dependencies = null;
            Tags = null;
            SupportedOSes = OSEnum.Windows;

            Url = null;
            InstallFolder = null;
            ConfigFile = null;
            FilesToDelete = null;
            FilesToBackup = null;
            FilesToPatch = null;
            RunAfterInstall = null;
            MD5 = null;
            SharedFix = null;
            SharedFixInstallFolder = null;
        }

        [SetsRequiredMembers]
        public FileFixEntity(BaseFixEntity fix)
        {
            Name = fix.Name;
            Version = fix.Version;
            Guid = fix.Guid;
            Description = fix.Description;
            Dependencies = fix.Dependencies;
            Tags = fix.Tags;
            SupportedOSes = fix.SupportedOSes;

            Url = null;
            InstallFolder = null;
            ConfigFile = null;
            FilesToDelete = null;
            FilesToBackup = null;
            RunAfterInstall = null;
            MD5 = null;
            SharedFix = null;
            SharedFixInstallFolder = null;
        }

        /// <summary>
        /// Download URL
        /// </summary>
        public string? Url { get; set; }

        /// <summary>
        /// List of fix's variants
        /// Names of folders inside a fix's archive
        /// </summary>
        public List<string>? Variants { get; set; }

        /// <summary>
        /// Folder to unpack archive
        /// Relative to the game folder
        /// </summary>
        public string? InstallFolder { get; set; }

        /// <summary>
        /// Fix configuration file
        /// Can be any file including .exe
        /// Path is relative to the game folder
        /// </summary>
        public string? ConfigFile { get; set; }

        /// <summary>
        /// List of files that will be backed up and deleted before the fix is installed
        /// Paths are relative to the game folder
        /// </summary>
        public List<string>? FilesToDelete { get; set; }

        /// <summary>
        /// List of files that will be backed up before the fix is installed, and the original file will remain
        /// Paths are relative to the game folder
        /// </summary>
        public List<string>? FilesToBackup { get; set; }

        /// <summary>
        /// File that will be run after the fix is installed
        /// Path is relative to the game folder
        /// </summary>
        public string? RunAfterInstall { get; set; }

        /// <summary>
        /// Zip archive MD5
        /// </summary>
        public string? MD5 { get; set; }

        /// <summary>
        /// List of files that will be backed up and patched with filename.octodiff patch
        /// </summary>
        public List<string>? FilesToPatch { get; set; }

        /// <summary>
        /// Guid of the fix that will be installed alongside
        /// </summary>
        public Guid? SharedFixGuid { get; set; }

        /// <summary>
        /// Folder to unpack shared fix to
        /// Relative to the game folder
        /// </summary>
        public string? SharedFixInstallFolder { get; set; }

        [JsonIgnore]
        public FileFixEntity? SharedFix { get; set; }

        /// <summary>
        /// Is there a newer version of the fix or shared fix
        /// </summary>
        [JsonIgnore]
        public override bool IsOutdated
        {
            get
            {
                if (InstalledFix is not null && InstalledFix.Version < Version)
                {
                    return true;
                }

                if (SharedFix is not null && SharedFix.IsOutdated)
                {
                    return true;
                }

                return false;
            }
        }
    }
}
