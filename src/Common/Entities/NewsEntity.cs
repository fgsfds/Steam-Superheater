using System.Text.Json.Serialization;

namespace Common.Entities
{
    public sealed class NewsEntity()
    {
        /// <summary>
        /// Date of the news article
        /// </summary>
        public DateTime Date { get; init; }

        /// <summary>
        /// News article text
        /// </summary>
        public string Content { get; init; }

        /// <summary>
        /// Is newer than the last read version
        /// </summary>
        [JsonIgnore]
        public bool IsNewer { get; set; }
    }

    [JsonSourceGenerationOptions(WriteIndented = true)]
    [JsonSerializable(typeof(List<NewsEntity>))]
    internal sealed partial class NewsEntityContext : JsonSerializerContext { }
}
