using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Common.Axiom.Enums;

namespace Common.Axiom.Entities.Fixes.RegistryFix;

public sealed class RegistryFixEntity : BaseFixEntity
{
    public RegistryFixEntity()
    {
    }

    [SetsRequiredMembers]
    public RegistryFixEntity(bool _)
    {
        Name = string.Empty;
        Version = "1.0";
        Guid = Guid.NewGuid();
        Description = null;
        Changelog = null;
        Dependencies = null;
        Tags = null;
        SupportedOSes = OSEnum.Windows;
        IsDisabled = false;

        Entries = [new()];
    }

    [SetsRequiredMembers]
    public RegistryFixEntity(BaseFixEntity fix)
    {
        Name = fix.Name;
        Version = fix.Version;
        Guid = fix.Guid;
        Description = fix.Description;
        Changelog = fix.Changelog;
        Dependencies = fix.Dependencies;
        Tags = fix.Tags;
        SupportedOSes = OSEnum.Windows;
        IsDisabled = fix.IsDisabled;

        Entries = [new()];
    }

    public required List<RegistryEntry> Entries { get; set; }

    [JsonIgnore]
    public override bool DoesRequireAdminRights => Entries.Exists(x => x.Key.StartsWith("HKEY_LOCAL_MACHINE", StringComparison.OrdinalIgnoreCase));
}

public sealed class RegistryEntry
{
    /// <summary>
    /// Registry key
    /// </summary>
    public required string Key { get; set; }

    /// <summary>
    /// Registry value name
    /// </summary>
    public required string ValueName { get; set; }

    /// <summary>
    /// Registry value
    /// </summary>
    public required string NewValueData { get; set; }

    /// <summary>
    /// Value type
    /// </summary>
    public required RegistryValueTypeEnum ValueType { get; set; }

    [JsonIgnore]
    public bool IsString
    {
        get
        {
            return ValueType is RegistryValueTypeEnum.String;
        }
        set
        {
            if (value)
            {
                ValueType = RegistryValueTypeEnum.String;
            }
            else
            {
                ValueType = RegistryValueTypeEnum.Dword;
            }
        }
    }

    [JsonIgnore]
    public bool IsDword
    {
        get
        {
            return ValueType is RegistryValueTypeEnum.Dword;
        }
        set
        {
            if (value)
            {
                ValueType = RegistryValueTypeEnum.Dword;
            }
            else
            {
                ValueType = RegistryValueTypeEnum.String;
            }
        }
    }

    [SetsRequiredMembers]
    public RegistryEntry()
    {
        Key = string.Empty;
        ValueName = string.Empty;
        NewValueData = string.Empty;
        ValueType = RegistryValueTypeEnum.String;
    }
}
