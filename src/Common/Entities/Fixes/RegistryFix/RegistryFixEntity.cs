using Common.Enums;

namespace Common.Entities.Fixes.RegistryFix
{
    public sealed class RegistryFixEntity : BaseFixEntity
    {
        public RegistryFixEntity()
        {
            Name = string.Empty;
            Version = 1;
            Guid = Guid.NewGuid();
            Description = null;
            Dependencies = null;
            Tags = null;
            SupportedOSes = OSEnum.Windows;
        }

        public string Key { get; set; }

        public string ValueName { get; set; }

        public string NewValueData { get; set; }
    }
}
