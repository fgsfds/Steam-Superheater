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

            Key = string.Empty;
            ValueName = string.Empty;
            NewValueData = string.Empty;
            ValueType = RegistryValueType.String;
        }

        public RegistryFixEntity(BaseFixEntity fix)
        {
            Name = fix.Name;
            Version = fix.Version;
            Guid = fix.Guid;
            Description = fix.Description;
            Dependencies = fix.Dependencies;
            Tags = fix.Tags;
            SupportedOSes = OSEnum.Windows;

            Key = string.Empty;
            ValueName = string.Empty;
            NewValueData = string.Empty;
            ValueType = RegistryValueType.String;
        }

        public string Key { get; set; }

        public string ValueName { get; set; }

        public string NewValueData { get; set; }

        public RegistryValueType ValueType { get; set; }
    }

    public enum RegistryValueType
    {
        String,
        Dword
    }
}
