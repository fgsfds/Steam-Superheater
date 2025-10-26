using System.Text.Json.Serialization;

namespace Common.Axiom.Entities;

public sealed class AppReleaseEntity
{
    /// <summary>
    /// Release version
    /// </summary>
    public required Version Version { get; init; }

    /// <summary>
    /// Release description
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Release download URL
    /// </summary>
    public required Uri DownloadUrl { get; init; }
}

[JsonSerializable(typeof(List<AppReleaseEntity>))]
public sealed partial class AppReleaseEntityContext : JsonSerializerContext;
