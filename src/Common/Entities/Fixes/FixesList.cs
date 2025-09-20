using Common.Enums;
using System.Text.Json.Serialization;

namespace Common.Entities.Fixes;

/// <summary>
/// Represents a source entity containing metadata and a list of fixes.
/// </summary>
public sealed class SourceEntity
{
    public string Name { get; set; } = string.Empty;

    public DateTime UpdatedTime { get; set; }

    public required List<FixesList> FixesList { get; set; }
}

/// <summary>
/// Represents a collection of fixes associated with a specific game, including metadata about the game and utility
/// properties for determining the state of the fixes.
/// </summary>
public sealed class FixesList
{
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
    public required List<BaseFixEntity> Fixes { get; set; }

    /// <summary>
    /// Game entity
    /// </summary>
    [JsonIgnore]
    public GameEntity? Game { get; set; }

    /// <summary>
    /// Does list have any not hidden fixes
    /// </summary>
    [JsonIgnore]
    public bool IsEmpty => Fixes.Count == 0 || !Fixes.Exists(static x => !x.IsHidden);

    /// <summary>
    /// Is game installed
    /// </summary>
    [JsonIgnore]
    public bool IsGameInstalled => Game is not null;

    /// <summary>
    /// Does this game have installed fixes
    /// </summary>
    [JsonIgnore]
    public bool HasInstalledFixes => Fixes.Exists(static x => x.IsInstalled);

    /// <summary>
    /// Does this game have newer version of fixes
    /// </summary>
    [JsonIgnore]
    public bool HasUpdates => Fixes.Any(static x => x.IsOutdated);
}


[JsonSourceGenerationOptions(
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    Converters = [typeof(JsonStringEnumConverter<OSEnum>), typeof(JsonStringEnumConverter<RegistryValueTypeEnum>)]
)]
[JsonSerializable(typeof(SourceEntity))]
public sealed partial class SourceEntityContext : JsonSerializerContext;

