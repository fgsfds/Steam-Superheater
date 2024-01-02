namespace Common.Entities.Fixes.RegistryFix
{
    public sealed class RegistryInstalledFixEntity() : BaseInstalledFixEntity
    {
        /// <summary>
        /// Registry key
        /// </summary>
        public required string Key { get; init; }

        /// <summary>
        /// Registry value name
        /// </summary>
        public required string ValueName { get; init; }

        /// <summary>
        /// Original value, null if the value was created
        /// </summary>
        public required string? OriginalValue { get; init; }
    }
}
