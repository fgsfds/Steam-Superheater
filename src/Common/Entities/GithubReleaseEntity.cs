#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
using System.Text.Json.Serialization;

namespace Common.Entities;

public sealed class GitHubReleaseEntity
{
    [JsonPropertyName("tag_name")]
    public string TagName { get; set; }

    [JsonPropertyName("draft")]
    public bool IsDraft { get; set; }

    [JsonPropertyName("prerelease")]
    public bool IsPrerelease { get; set; }

    [JsonPropertyName("assets")]
    public GitHubReleaseAsset[] Assets { get; set; }

    [JsonPropertyName("body")]
    public string Description { get; set; }
}

public sealed class GitHubReleaseAsset
{
    [JsonPropertyName("name")]
    public string FileName { get; set; }

    [JsonPropertyName("browser_download_url")]
    public string DownloadUrl { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedDate { get; set; }
}


[JsonSerializable(typeof(List<GitHubReleaseEntity>))]
public sealed partial class GitHubReleaseEntityContext : JsonSerializerContext;

