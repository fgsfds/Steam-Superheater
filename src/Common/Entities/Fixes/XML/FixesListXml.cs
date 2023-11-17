using System.Xml.Serialization;

namespace Common.Entities.Fixes.XML
{
    /// <summary>
    /// Entity containing game information and a list of fixes for it
    /// </summary>
    [XmlType("FixesList")]
    public sealed class FixesListXml()
    {
        public FixesListXml(
            int gameId,
            string gameName,
            List<object> fixes
            ) : this()
        {
            GameId = gameId;
            GameName = gameName;
            Fixes = fixes;
        }

        public FixesListXml(FixesList fix) : this()
        {
            GameId = fix.GameId;
            GameName = fix.GameName;
            Fixes = fix.Fixes.ConvertAll(x => (object)x);
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
        [XmlElement("FileFix", typeof(FileFixEntity))]
        public List<object> Fixes { get; init; }
    }
}
