using Common.Enums;

namespace Common.Entities.Fixes.RegistryFix;

[Obsolete("Remove when there's no versions <2.0.0")]
public sealed class RegistryInstalledFixEntity : BaseInstalledFixEntity
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

