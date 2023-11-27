using System.Xml.Serialization;

namespace Common.Entities
{
    [XmlRoot]
    public sealed class NewsEntity()
    {
        /// <summary>
        /// Date of the news article
        /// </summary>
        [XmlElement]
        public DateTime Date { get; init; }

        /// <summary>
        /// News article text
        /// </summary>
        [XmlElement]
        public string Content { get; init; }

        /// <summary>
        /// Is newer than the last read version
        /// </summary>
        [XmlIgnore]
        public bool IsNewer { get; set; }
    }
}
