namespace Common.Entities.Fixes.HostsFix
{
    public sealed class HostsInstalledFixEntity() : BaseInstalledFixEntity
    {
        /// <summary>
        /// List of entries that were added to the hosts file
        /// </summary>
        public required List<string> Entries { get; init; }
    }
}
