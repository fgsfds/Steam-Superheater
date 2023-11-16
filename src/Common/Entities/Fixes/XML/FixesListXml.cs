using System.Xml.Serialization;

namespace Common.Entities.Fixes.XML
{
    /// <summary>
    /// Entity containing game information and a list of fixes for it
    /// </summary>
    [XmlRoot("FixesList")]
    public sealed class FixesListXml()
    {
        public FixesListXml(
            int gameId,
            string gameName,
            List<object> fixes
            ) :this()
        {
            GameId = gameId;
            GameName = gameName;
            Fixes = fixes;
        }

        /// <summary>
        /// Steam ID of the game
        /// </summary>
        public required int GameId { get; init; }

        /// <summary>
        /// Game title
        /// </summary>
        public required string GameName { get; init; }

        /// <summary>
        /// List of fixes
        /// </summary>
        [XmlElement("FileFix", typeof(FileFixEntity))]
        public required List<object> Fixes { get; init; }
    }
}
