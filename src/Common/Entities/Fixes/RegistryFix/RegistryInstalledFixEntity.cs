namespace Common.Entities.Fixes.RegistryFix
{
    public sealed class RegistryInstalledFixEntity() : BaseInstalledFixEntity
    {
        required public string Key { get; init; }

        required public string ValueName { get; init; }

        required public string? OriginalValue { get; init; }
    }
}
