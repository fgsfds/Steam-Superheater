namespace Common.Entities.Fixes.RegistryFix
{
    public sealed class RegistryInstalledFixEntity() : BaseInstalledFixEntity
    {
        public RegistryInstalledFixEntity(
            int id,
            Guid guid,
            int version,
            string key,
            string valueName,
            string? originalValue) : this()
        {
            GameId = id;
            Guid = guid;
            Version = version;
            Key = key;
            ValueName = valueName;
            OriginalValue = originalValue;
        }

        public string Key { get; set; }

        public string ValueName { get; set; }

        public string? OriginalValue { get; init; }
    }
}
