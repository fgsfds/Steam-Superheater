using System.Xml.Serialization;

namespace Common.Entities.Fixes.FileFix
{
    //Obsolete, remove in version 1.0
    [XmlType("InstalledFixEntity")]
    public sealed class InstalledFixEntity_Obsolete() : BaseInstalledFixEntity
    {
        /// <summary>
        /// Name of the backup folder
        /// </summary>
        public string BackupFolder { get; init; }

        /// <summary>
        /// Paths to files relative to the game folder
        /// </summary>
        public List<string>? FilesList { get; init; }
    }
}
