using Common.Enums;
using System.Diagnostics.CodeAnalysis;

namespace Common.Entities.Fixes.HostsFix;

public sealed class HostsFixEntity : BaseFixEntity
{
    //[SetsRequiredMembers]
    public HostsFixEntity()
    {
        Name = string.Empty;
        Version = 1;
        VersionStr = null;
        Guid = Guid.NewGuid();
        Description = null;
        Changelog = null;
        Dependencies = null;
        Tags = null;
        SupportedOSes = OSEnum.Windows;
        IsDisabled = false;

        Entries = [];
    }

    [SetsRequiredMembers]
    public HostsFixEntity(BaseFixEntity fix)
    {
        Name = fix.Name;
        Version = fix.Version;
        VersionStr = null;
        Guid = fix.Guid;
        Description = fix.Description;
        Changelog = fix.Changelog;
        Dependencies = fix.Dependencies;
        Tags = fix.Tags;
        SupportedOSes = OSEnum.Windows;
        IsDisabled = fix.IsDisabled;

        Entries = [];
    }

    /// <summary>
    /// List of entries to be added to the hosts file
    /// </summary>
    public required List<string> Entries { get; set; }
}

