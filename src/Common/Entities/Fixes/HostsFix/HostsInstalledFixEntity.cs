namespace Common.Entities.Fixes.HostsFix
{
    public sealed class HostsInstalledFixEntity() : BaseInstalledFixEntity
    {
        public HostsInstalledFixEntity(
            int id,
            Guid guid,
            int version,
            List<string> entries) : this()
        {
            GameId = id;
            Guid = guid;
            Version = version;
            Entries = entries;
        }

        public List<string> Entries { get; set; }
    }
}
