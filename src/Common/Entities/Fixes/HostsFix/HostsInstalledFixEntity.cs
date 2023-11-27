namespace Common.Entities.Fixes.HostsFix
{
    public sealed class HostsInstalledFixEntity() : BaseInstalledFixEntity
    {
        public required List<string> Entries { get; init; }
    }
}
