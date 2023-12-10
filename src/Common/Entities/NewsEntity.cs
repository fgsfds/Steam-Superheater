using System.Text.Json.Serialization;
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
        [JsonIgnore]
        public bool IsNewer { get; set; }
    }

    [JsonSourceGenerationOptions(WriteIndented = true)]
    [JsonSerializable(typeof(List<NewsEntity>))]
    internal sealed partial class NewsEntityContext : JsonSerializerContext { }
}
