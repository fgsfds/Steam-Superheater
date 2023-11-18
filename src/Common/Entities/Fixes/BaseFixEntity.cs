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
        public string Name { get; set; }

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

        /// <summary>
        /// Supported OSes
        /// </summary>
        public OSEnum SupportedOSes { get; set; }

        /// <summary>
        /// List of files that will be backed up before the fix is installed, and the original file will remain
        /// Paths are relative to the game folder, separated by ;
        /// </summary>
        public List<string>? Tags { get; set; }

        /// <summary>
        /// Fix version
        /// </summary>
        public int Version { get; set; }

        public override string ToString() => Name;
    }
}