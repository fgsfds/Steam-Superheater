using Common.Entities.Fixes.RegistryFix;
using Common.Enums;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Common.Entities.Fixes.RegistryFixV2;

public sealed class RegistryFixV2Entity : BaseFixEntity
{
    [SetsRequiredMembers]
    public RegistryFixV2Entity()
    {
        Name = string.Empty;
        Version = 1;
        Guid = Guid.NewGuid();
        Description = null;
        Dependencies = null;
        Tags = null;
        SupportedOSes = OSEnum.Windows;
        IsDisabled = false;

        Entries = [new()];
    }

    [SetsRequiredMembers]
    public RegistryFixV2Entity(BaseFixEntity fix)
    {
        Name = fix.Name;
        Version = fix.Version;
        Guid = fix.Guid;
        Description = fix.Description;
        Dependencies = fix.Dependencies;
        Tags = fix.Tags;
        SupportedOSes = OSEnum.Windows;
        IsDisabled = fix.IsDisabled;

        Entries = [new()];
    }

    [SetsRequiredMembers]
    public RegistryFixV2Entity(RegistryFixEntity fix)
    {
        Name = fix.Name;
        Version = fix.Version;
        Guid = fix.Guid;
        Description = fix.Description;
        Dependencies = fix.Dependencies;
        Tags = fix.Tags;
        SupportedOSes = OSEnum.Windows;
        IsDisabled = fix.IsDisabled;

        Entries = [new() { Key = fix.Key, NewValueData = fix.NewValueData, ValueName = fix.ValueName, ValueType = fix.ValueType}];
    }

    public required List<RegistryEntry> Entries { get; set; }
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
        ValueType = RegistryValueTypeEnum.String;
    }
}
