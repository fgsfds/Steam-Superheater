using Common.Enums;

namespace Common.Entities.Fixes.HostsFix
{
    public sealed class HostsFixEntity : BaseFixEntity
    {
        public HostsFixEntity()
        {
            Name = string.Empty;
            Version = 1;
            Guid = Guid.NewGuid();
            Description = null;
            Dependencies = null;
            Tags = null;
            SupportedOSes = OSEnum.Windows;

            Entries = new();
        }

        public HostsFixEntity(BaseFixEntity fix)
        {
            Name = fix.Name;
            Version = fix.Version;
            Guid = fix.Guid;
            Description = fix.Description;
            Dependencies = fix.Dependencies;
            Tags = fix.Tags;
            SupportedOSes = OSEnum.Windows;

            Entries = new();
        }

        public List<string> Entries { get; set; }
    }
}
