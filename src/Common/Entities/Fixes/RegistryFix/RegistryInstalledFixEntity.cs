namespace Common.Entities.Fixes.RegistryFix
{
    public sealed class RegistryInstalledFixEntity() : BaseInstalledFixEntity
    {
        public required string Key { get; init; }

        public required string ValueName { get; init; }

        public required string? OriginalValue { get; init; }
    }
}
