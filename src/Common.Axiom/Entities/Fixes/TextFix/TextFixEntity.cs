using System.Diagnostics.CodeAnalysis;
using Common.Axiom.Enums;

namespace Common.Axiom.Entities.Fixes.TextFix;

public sealed class TextFixEntity : BaseFixEntity
{
    public TextFixEntity()
    {
    }

    [SetsRequiredMembers]
    public TextFixEntity(bool _)
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
    }

    [SetsRequiredMembers]
    public TextFixEntity(BaseFixEntity fix)
    {
        Name = fix.Name;
        Version = fix.Version;
        Guid = fix.Guid;
        Description = fix.Description;
        Changelog = fix.Changelog;
        Dependencies = null;
        Tags = fix.Tags;
        SupportedOSes = OSEnum.Windows;
        IsDisabled = fix.IsDisabled;
    }
}

