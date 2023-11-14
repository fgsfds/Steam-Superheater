using Common.Enums;
using System.Xml.Serialization;

namespace Common.Entities.Fixes
{
    /// <summary>
    /// Fix entity
    /// </summary>
    public sealed partial class FileFixEntity : IFixEntity
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
        /// List of fixes GUIDs that are required for this fix
        /// </summary>
        public List<Guid>? Dependencies { get; set; }

        /// <summary>
        /// Fix description
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Fix GUID
        /// </summary>
        public Guid Guid { get; init; }

        /// <summary>
        /// Is there a newer version of the fix
        /// </summary>
        [XmlIgnore]
        public bool HasNewerVersion => InstalledFix is not null && InstalledFix.Version < Version;

        /// <summary>
        /// Installed fix entity
        /// </summary>
        public IInstalledFixEntity? InstalledFix { get; set; }

        /// <summary>
        /// Is this fix hidden from the list
        /// </summary>
        [XmlIgnore]
        public bool IsHidden { get; set; }

        /// <summary>
        /// Is fix installed
        /// </summary>
        [XmlIgnore]
        public bool IsInstalled => InstalledFix is not null;

        /// <summary>
        /// Fix title
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// List of files that will be backed up before the fix is installed, and the original file will remain
        /// Paths are relative to the game folder, separated by ;
        /// </summary>
        public List<string>? Tags { get; set; }

        /// <summary>
        /// Fix version
        /// </summary>
        public int Version { get; set; }

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
        /// Supported OSes
        /// </summary>
        public OSEnum SupportedOSes { get; set; }

        /// <summary>
        /// Zip archive MD5
        /// </summary>
        public string? MD5 { get; set; }
    }
}
