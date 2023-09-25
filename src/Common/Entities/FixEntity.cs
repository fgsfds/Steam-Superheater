using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace SteamFDCommon.Entities
{
    /// <summary>
    /// Entity containing game information and a list of fixes for it
    /// </summary>
    public class FixesList
    {
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
        public ObservableCollection<FixEntity> Fixes { get; init; }

        public FixesList(
            int gameId,
            string gameName,
            ObservableCollection<FixEntity> fixes
            )
        {
            GameId = gameId;
            GameName = gameName;
            Fixes = fixes;
        }

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
    public partial class FixEntity : ObservableObject
    {
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
        public string Url { get; set; }

        /// <summary>
        /// Fix description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// List of fix's variants
        /// Names of folders in inside a fix's archive, separated by ;
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
        public string? FilesToDelete { get; set; }

        /// <summary>
        /// List of files that will be backed up before the fix is installed, and the original file will remain
        /// Paths are relative to the game folder, separated by ;
        /// </summary>
        public string? FilesToBackup { get; set; }

        /// <summary>
        /// File that will be run after the fix is installed
        /// Path is relative to the game folder
        /// </summary>
        public string? RunAfterInstall { get; set; }

        /// <summary>
        /// List of fixes GUIDs that are required for this fix
        /// </summary>
        public List<Guid> Dependencies { get; set; }

        /// <summary>
        /// Is fix installed
        /// </summary>
        public bool IsInstalled => InstalledFix is not null;

        /// <summary>
        /// Is there a newer version of the fix
        /// </summary>
        public bool HasNewerVersion => InstalledFix is not null && InstalledFix.Version < Version;

        public FixEntity()
        {
            Name = string.Empty;
            Version = 1;
            Url = string.Empty;
            Description = string.Empty;
            Guid = Guid.NewGuid();
            InstallFolder = null;
            ConfigFile = null;
            FilesToDelete = null;
            FilesToBackup = null;
            RunAfterInstall = null;
            Dependencies = new();
        }

        public override string ToString() => Name;
    }
}
