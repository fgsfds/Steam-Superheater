using Common.Enums;

namespace Common.Entities
{
    public interface IFixEntity
    {
        string Name { get; set; }
        List<Guid>? Dependencies { get; set; }
        string? Description { get; set; }
        Guid Guid { get; init; }
        bool HasNewerVersion { get; }
        bool IsHidden { get; set; }
        bool IsInstalled { get; }
        OSEnum SupportedOSes { get; set; }
        List<string>? Tags { get; set; }

        string? ToString() => Name;
    }
}