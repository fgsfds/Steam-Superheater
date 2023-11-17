namespace Common.Entities.Fixes.RegistryFix
{
    public sealed class RegistryInstalledFixEntity() : BaseInstalledFixEntity
    {
        public RegistryInstalledFixEntity(
            int id,
            Guid guid,
            int version,
            string originalValue) : this()
        {
            GameId = id;
            Guid = guid;
            Version = version;
            OriginalValue = originalValue;
        }

        public string OriginalValue { get; init; }
    }
}
