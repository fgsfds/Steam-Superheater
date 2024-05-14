using Common.Enums;
using Common.Helpers;
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
            FileSize = null;
            InstallFolder = null;
            ConfigFile = null;
            FilesToDelete = null;
            FilesToBackup = null;
            FilesToPatch = null;
            RunAfterInstall = null;
            MD5 = null;
            SharedFixGuid = null;
            SharedFix = null;
            SharedFixInstallFolder = null;
            WineDllOverrides = null;
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
            FileSize = null;
            InstallFolder = null;
            ConfigFile = null;
            FilesToDelete = null;
            FilesToBackup = null;
            RunAfterInstall = null;
            MD5 = null;
            SharedFixGuid = null;
            SharedFix = null;
            SharedFixInstallFolder = null;
            WineDllOverrides = null;
        }

        /// <summary>
        /// Download URL
        /// </summary>
        public string? Url { get; set; }

        public long? FileSize {  get; set; }

        /// <summary>
        /// List of fix's variants
        /// Names of folders inside a fix's archive
        /// </summary>
        public List<string>? Variants { get; set; }

        /// <summary>
        /// Folder to unpack archive
        /// Relative to the game folder
        /// </summary>
        private string? _installFolder;
        public string? InstallFolder
        { 
            get => _installFolder?.ReplaceDirectorySeparatorChar();
            set => _installFolder = value; 
        }

        /// <summary>
        /// Fix configuration file
        /// Can be any file including .exe
        /// Path is relative to the game folder
        /// </summary>
        private string? _configFile;
        public string? ConfigFile
        {
            get => _configFile?.ReplaceDirectorySeparatorChar();
            set => _configFile = value;
        }

        /// <summary>
        /// List of files that will be backed up and deleted before the fix is installed
        /// Paths are relative to the game folder
        /// </summary>
        private List<string>? _filesToDelete;
        public List<string>? FilesToDelete
        {
            get => _filesToDelete?.ConvertAll(static x => x.ReplaceDirectorySeparatorChar());
            set => _filesToDelete = value;
        }

        /// <summary>
        /// List of files that will be backed up before the fix is installed, and the original file will remain
        /// Paths are relative to the game folder
        /// </summary>
        private List<string>? _filesToBackup;
        public List<string>? FilesToBackup
        {
            get => _filesToBackup?.ConvertAll(static x => x.ReplaceDirectorySeparatorChar());
            set => _filesToBackup = value;
        }

        /// <summary>
        /// File that will be run after the fix is installed
        /// Path is relative to the game folder
        /// </summary>
        private string? _runAfterInstall;
        public string? RunAfterInstall
        {
            get => _runAfterInstall?.ReplaceDirectorySeparatorChar();
            set => _runAfterInstall = value;
        }

        /// <summary>
        /// Zip archive MD5
        /// </summary>
        public string? MD5 { get; set; }

        /// <summary>
        /// List of files that will be backed up and patched with filename.octodiff patch
        /// </summary>
        private List<string>? _filesToPatch;
        public List<string>? FilesToPatch
        {
            get => _filesToPatch?.ConvertAll(static x => x.ReplaceDirectorySeparatorChar());
            set => _filesToPatch = value;
        }

        /// <summary>
        /// Guid of the fix that will be installed alongside
        /// </summary>
        public Guid? SharedFixGuid { get; set; }

        /// <summary>
        /// Folder to unpack shared fix to
        /// Relative to the game folder
        /// </summary>
        private string? _sharedFixInstallFolder;
        public string? SharedFixInstallFolder
        {
            get => _sharedFixInstallFolder?.ReplaceDirectorySeparatorChar();
            set => _sharedFixInstallFolder = value;
        }

        /// <summary>
        /// Dlls that will be added to windlloverrides
        /// </summary>
        public List<string>? WineDllOverrides { get; set; }

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

        public FileFixEntity Clone()
        {
            return new()
            {
                Name = this.Name,
                Version = this.Version,
                Guid = this.Guid,
                Description = this.Description,
                Dependencies = this.Dependencies,
                Tags = this.Tags,
                SupportedOSes = this.SupportedOSes,
                Url = this.Url,
                InstallFolder = this.InstallFolder,
                ConfigFile = this.ConfigFile,
                FilesToDelete = this.FilesToDelete,
                FilesToBackup = this.FilesToBackup,
                FilesToPatch = this.FilesToPatch,
                RunAfterInstall = this.RunAfterInstall,
                MD5 = this.MD5,
                SharedFixGuid = this.SharedFixGuid,
                SharedFix = this.SharedFix,
                SharedFixInstallFolder = this.SharedFixInstallFolder,
                WineDllOverrides = this.WineDllOverrides,
                FileSize = this.FileSize
            };
        }
    }
}
