using Common.Entities.Fixes.FileFix;
using Common.Entities.Fixes.HostsFix;
using Common.Entities.Fixes.RegistryFix;
using Common.Entities.Fixes.TextFix;
using Common.Enums;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace Common.Entities.Fixes
{
    /// <summary>
    /// Base fix entity
    /// </summary>
    [JsonDerivedType(typeof(FileFixEntity), typeDiscriminator: "FileFix")]
    [JsonDerivedType(typeof(HostsFixEntity), typeDiscriminator: "HostsFix")]
    [JsonDerivedType(typeof(RegistryFixEntity), typeDiscriminator: "RegistryFix")]
    [JsonDerivedType(typeof(TextFixEntity), typeDiscriminator: "TextFix")]
    public abstract class BaseFixEntity
    {
        /// <summary>
        /// Fix title
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// Fix GUID
        /// </summary>
        public required Guid Guid { get; init; }

        /// <summary>
        /// Fix version
        /// </summary>
        public required int Version { get; set; }

        /// <summary>
        /// Supported OSes
        /// </summary>
        public required OSEnum SupportedOSes { get; set; }

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
        [JsonIgnore]
        public bool HasNewerVersion => InstalledFix is not null && InstalledFix.Version < Version;

        /// <summary>
        /// Installed fix entity
        /// </summary>
        [XmlIgnore]
        [JsonIgnore]
        public BaseInstalledFixEntity? InstalledFix { get; set; }

        /// <summary>
        /// Is this fix hidden from the list
        /// </summary>
        [XmlIgnore]
        [JsonIgnore]
        public bool IsHidden { get; set; }

        /// <summary>
        /// Is fix installed
        /// </summary>
        [XmlIgnore]
        [JsonIgnore]
        public bool IsInstalled => InstalledFix is not null;

        public override string ToString() => Name;
    }
}