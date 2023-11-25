using Common.Enums;
using System.Xml.Serialization;

namespace Common.Entities.Fixes
{
    /// <summary>
    /// Base fix entity
    /// </summary>
    public abstract class BaseFixEntity
    {
        /// <summary>
        /// Fix title
        /// </summary>
        required public string Name { get; set; }

        /// <summary>
        /// Fix GUID
        /// </summary>
        required public Guid Guid { get; init; }

        /// <summary>
        /// Fix version
        /// </summary>
        required public int Version { get; set; }

        /// <summary>
        /// Supported OSes
        /// </summary>
        required public OSEnum SupportedOSes { get; set; }

        /// <summary>
        /// List of fixes GUIDs that are required for this fix
        /// </summary>
        public List<Guid>? Dependencies { get; set; }

        /// <summary>
        /// Fix description
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// List of files that will be backed up before the fix is installed, and the original file will remain
        /// Paths are relative to the game folder, separated by ;
        /// </summary>
        public List<string>? Tags { get; set; }

        /// <summary>
        /// Is there a newer version of the fix
        /// </summary>
        [XmlIgnore]
        public bool HasNewerVersion => InstalledFix is not null && InstalledFix.Version < Version;

        /// <summary>
        /// Installed fix entity
        /// </summary>
        [XmlIgnore]
        public BaseInstalledFixEntity? InstalledFix { get; set; }

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

        public override string ToString() => Name;
    }
}