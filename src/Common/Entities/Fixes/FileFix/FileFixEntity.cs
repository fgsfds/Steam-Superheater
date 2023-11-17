using Common.Enums;
using System.Xml.Serialization;

namespace Common.Entities.Fixes
{
    /// <summary>
    /// Fix entity
    /// </summary>
    public sealed class FileFixEntity : BaseFixEntity
    {
        public FileFixEntity()
        {
            Name = string.Empty;
            Version = 1;
            Guid = Guid.NewGuid();
            SupportedOSes = OSEnum.Windows;
            Url = null;
            Description = null;
            InstallFolder = null;
            ConfigFile = null;
            FilesToDelete = null;
            FilesToBackup = null;
            RunAfterInstall = null;
            Dependencies = null;
            Tags = null;
            MD5 = null;
        }

        /// <summary>
        /// Download URL
        /// </summary>
        public string? Url { get; set; }

        /// <summary>
        /// List of fix's variants
        /// Names of folders inside a fix's archive, separated by ;
        /// </summary>
        public List<string>? Variants { get; set; }

        /// <summary>
        /// Folder to unpack ZIP
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
        /// Paths are relative to the game folder, separated by ;
        /// </summary>
        public List<string>? FilesToDelete { get; set; }

        /// <summary>
        /// List of files that will be backed up before the fix is installed, and the original file will remain
        /// Paths are relative to the game folder, separated by ;
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
    }
}
