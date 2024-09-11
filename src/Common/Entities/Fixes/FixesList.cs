using Common.Enums;
using System.Text.Json.Serialization;

namespace Common.Entities.Fixes;

/// <summary>
/// Entity containing game information and a list of fixes for it
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
[JsonSerializable(typeof(List<FixesList>))]
public sealed partial class FixesListContext : JsonSerializerContext;

