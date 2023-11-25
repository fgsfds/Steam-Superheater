using Common.Entities.Fixes.FileFix;
using Common.Entities.Fixes.HostsFix;
using Common.Entities.Fixes.RegistryFix;
using System.Xml.Serialization;

namespace Common.Entities.Fixes.XML
{
    /// <summary>
    /// Entity containing game information and a list of fixes for it
    /// </summary>
    [XmlType("FixesList")]
    public sealed class FixesListXml()
    {
        public FixesListXml(FixesList fix) : this()
        {
            GameId = fix.GameId;
            GameName = fix.GameName;
            Fixes = fix.Fixes.ConvertAll(x => (object)x);
        }

        /// <summary>
        /// Steam ID of the game
        /// </summary>
        [XmlElement]
        public int GameId { get; init; }

        /// <summary>
        /// Game title
        /// </summary>
        [XmlElement]
        public string GameName { get; init; }

        /// <summary>
        /// List of fixes
        /// </summary>
        [XmlElement("FileFix", typeof(FileFixEntity))]
        [XmlElement("RegistryFix", typeof(RegistryFixEntity))]
        [XmlElement("HostsFix", typeof(HostsFixEntity))]
        public List<object> Fixes { get; init; }
    }
}
