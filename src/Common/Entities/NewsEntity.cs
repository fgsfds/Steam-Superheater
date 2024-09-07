using System.Text.Json.Serialization;

namespace Common.Entities;

public sealed class NewsEntity
{
    /// <summary>
    /// Date of the news article
    /// </summary>
    public required DateTime Date { get; init; }

    /// <summary>
    /// News article text
    /// </summary>
    public required string Content { get; set; }

    /// <summary>
    /// Is newer than the last read version
    /// </summary>
    [JsonIgnore]
    public bool IsNewer { get; set; }

    public override string ToString()
    {
        return $"""
            ##### {Date:dd.MM.yy}

            {Content}
            """;
    }
}

[JsonSerializable(typeof(List<NewsEntity>))]
public sealed partial class NewsEntityContext : JsonSerializerContext;

