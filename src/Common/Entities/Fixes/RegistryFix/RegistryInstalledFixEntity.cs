using Common.Enums;
using System.Text.Json.Serialization;

namespace Common.Entities.Fixes.RegistryFix;

public sealed class RegistryInstalledFixEntity : BaseInstalledFixEntity
{
    public required List<RegistryInstalledEntry> Entries { get; set; }

    [JsonIgnore]
    public override bool DoesRequireAdminRights => Entries.Exists(x => x.Key.StartsWith("HKEY_LOCAL_MACHINE", StringComparison.OrdinalIgnoreCase));
}

public sealed class RegistryInstalledEntry
{
    /// <summary>
    /// Registry key
    /// </summary>
    public required string Key { get; init; }

    /// <summary>
    /// Registry value name
    /// </summary>
    public required string ValueName { get; init; }

    /// <summary>
    /// Value type
    /// </summary>
    public required RegistryValueTypeEnum ValueType { get; init; }

    /// <summary>
    /// Original value, null if the value was created
    /// </summary>
    public string? OriginalValue { get; init; }
}
