using System.Xml.Serialization;

namespace Common.Entities
{
    [XmlRoot]
    public sealed partial class NewsEntity()
    {
        /// <summary>
        /// Date of the news article
        /// </summary>
        [XmlElement]
        public DateTime Date { get; set; }

        /// <summary>
        /// News article text
        /// </summary>
        [XmlElement]
        public string Content { get; set; }

        /// <summary>
        /// Is newer than the last read version
        /// </summary>
        [XmlElement]
        public bool IsNewer { get; set; }
    }
}
