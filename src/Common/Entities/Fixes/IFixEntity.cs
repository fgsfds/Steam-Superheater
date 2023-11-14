
using Common.Enums;

namespace Common.Entities.Fixes
{
    public interface IFixEntity
    {
        List<Guid>? Dependencies { get; set; }
        string? Description { get; set; }
        Guid Guid { get; init; }
        bool HasNewerVersion { get; }
        IInstalledFixEntity? InstalledFix { get; set; }
        bool IsHidden { get; set; }
        bool IsInstalled { get; }
        string Name { get; set; }
        List<string>? Tags { get; set; }
        int Version { get; set; }
        OSEnum SupportedOSes { get; set; }
    }
}