using Common.Enums;
using System.Diagnostics.CodeAnalysis;

namespace Common.Entities.Fixes.HostsFix
{
    public sealed class HostsFixEntity : BaseFixEntity
    {
        [SetsRequiredMembers]
        public HostsFixEntity()
        {
            Name = string.Empty;
            Version = 1;
            Guid = Guid.NewGuid();
            Description = null;
            Dependencies = null;
            Tags = null;
            SupportedOSes = OSEnum.Windows;

            Entries = [];
        }

        [SetsRequiredMembers]
        public HostsFixEntity(BaseFixEntity fix)
        {
            Name = fix.Name;
            Version = fix.Version;
            Guid = fix.Guid;
            Description = fix.Description;
            Dependencies = fix.Dependencies;
            Tags = fix.Tags;
            SupportedOSes = OSEnum.Windows;

            Entries = [];
        }

        public List<string> Entries { get; set; }
    }
}
