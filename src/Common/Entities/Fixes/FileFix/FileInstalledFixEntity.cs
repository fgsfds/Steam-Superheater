using System.Text.Json;
using System.Text.Json.Serialization;

namespace Common.Entities.Fixes.FileFix;

public sealed class FileInstalledFixEntity : BaseInstalledFixEntity
{
    /// <summary>
    /// Name of the backup folder
    /// </summary>
    public string? BackupFolder { get; init; }

    /// <summary>
    /// Paths to files relative to the game folder and their hashes
    /// </summary>
    [JsonConverter(typeof(FilesListJsonConverter))]
    public Dictionary<string, long?>? FilesList { get; init; }

    public FileInstalledFixEntity? InstalledSharedFix { get; init; }

    public List<string>? WineDllOverrides { get; init; }
}


public class FilesListJsonConverter : JsonConverter<Dictionary<string, long?>?>
{
    public override Dictionary<string, long?>? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType is JsonTokenType.StartArray)
        {
            // Old format: ["file1", "file2", ...]
            var list = JsonSerializer.Deserialize<List<string>>(ref reader, options);
            return list?.ToDictionary(f => f, _ => (long?)null);
        }

        if (reader.TokenType is JsonTokenType.StartObject)
        {
            // New format: { "file1": 123, "file2": null, ... }
            return JsonSerializer.Deserialize<Dictionary<string, long?>>(ref reader, options);
        }

        throw new JsonException($"Unexpected token {reader.TokenType}");
    }

    public override void Write(
        Utf8JsonWriter writer,
        Dictionary<string, long?>? value,
        JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStartObject();

        foreach (var kvp in value)
        {
            if (kvp.Value.HasValue)
            {
                writer.WriteNumber(kvp.Key, kvp.Value.Value);
            }
            else
            {
                writer.WriteNull(kvp.Key);
            }

        }
        writer.WriteEndObject();
    }
}
