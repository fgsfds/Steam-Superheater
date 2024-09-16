using Common.Enums;

namespace Common.Entities.Fixes.RegistryFixV2;

public sealed class RegistryInstalledFixV2Entity : BaseInstalledFixEntity
{
    public required List<RegistryInstalledEntry> Entries { get; set; }
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
