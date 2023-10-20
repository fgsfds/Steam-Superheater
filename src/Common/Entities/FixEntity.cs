using Common.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Xml.Serialization;

namespace Common.Entities
{
    /// <summary>
    /// Entity containing game information and a list of fixes for it
    /// </summary>
    public sealed class FixesList
    {
        public FixesList(
            int gameId,
            string gameName,
            List<FixEntity> fixes
            )
        {
            GameId = gameId;
            GameName = gameName;
            Fixes = fixes;
        }

        /// <summary>
        /// Steam ID of the game
        /// </summary>
        public int GameId { get; init; }

        /// <summary>
        /// Game title
        /// </summary>
        public string GameName { get; init; }

        /// <summary>
        /// List of fixes
        /// </summary>
        public List<FixEntity> Fixes { get; init; }

        /// <summary>
        /// Serializer constructor
        /// </summary>
        private FixesList()
        {
        }
    }

    /// <summary>
    /// Fix entity
    /// </summary>
    public sealed partial class FixEntity : ObservableObject
    {
        public FixEntity()
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
        }

        /// <summary>
        /// Fix title
        /// </summary>
        [ObservableProperty]
        private string _name;

        /// <summary>
        /// Fix version
        /// </summary>
        [ObservableProperty]
        private int _version;

        /// <summary>
        /// Installed fix entity
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsInstalled))]
        [NotifyPropertyChangedFor(nameof(HasNewerVersion))]
        private InstalledFixEntity? _installedFix;

        /// <summary>
        /// Fix GUID
        /// </summary>
        public Guid Guid { get; init; }

        /// <summary>
        /// Download URL
        /// </summary>
        public string? Url { get; set; }

        /// <summary>
        /// Fix description
        /// </summary>
        public string? Description { get; set; }

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
        /// List of files that will be backed up before the fix is installed, and the original file will remain
        /// Paths are relative to the game folder, separated by ;
        /// </summary>
        public List<string>? Tags { get; set; }

        /// <summary>
        /// File that will be run after the fix is installed
        /// Path is relative to the game folder
        /// </summary>
        public string? RunAfterInstall { get; set; }

        /// <summary>
        /// List of fixes GUIDs that are required for this fix
        /// </summary>
        public List<Guid>? Dependencies { get; set; }

        /// <summary>
        /// Supported OSes
        /// </summary>
        public OSEnum SupportedOSes { get; set; }

        /// <summary>
        /// Is fix installed
        /// </summary>
        public bool IsInstalled => InstalledFix is not null;

        /// <summary>
        /// Is there a newer version of the fix
        /// </summary>
        public bool HasNewerVersion => InstalledFix is not null && InstalledFix.Version < Version;

        [XmlIgnore]
        public bool IsHidden { get; set; }

        public override string ToString() => Name;
    }
}
