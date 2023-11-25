namespace Common.Entities.Fixes.HostsFix
{
    public sealed class HostsInstalledFixEntity() : BaseInstalledFixEntity
    {
        required public List<string> Entries { get; init; }
    }
}
