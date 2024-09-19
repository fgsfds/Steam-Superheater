using Common.Enums;
using System.Diagnostics.CodeAnalysis;

namespace Common.Entities.Fixes.TextFix;

public sealed class TextFixEntity : BaseFixEntity
{
    //[SetsRequiredMembers]
    public TextFixEntity()
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
    }

    [SetsRequiredMembers]
    public TextFixEntity(BaseFixEntity fix)
    {
        Name = fix.Name;
        Version = fix.Version;
        VersionStr = null;
        Guid = fix.Guid;
        Description = fix.Description;
        Changelog = fix.Changelog;
        Dependencies = null;
        Tags = fix.Tags;
        SupportedOSes = OSEnum.Windows;
        IsDisabled = fix.IsDisabled;
    }
}

