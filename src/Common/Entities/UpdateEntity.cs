namespace Common.Entities
{
    public sealed class UpdateEntity
    {
        public Version Version { get; set; }

        public string Description { get; set; }

        public Uri DownloadUrl { get; set; }

        public UpdateEntity(
            Version version,
            string description,
            Uri url)
        {
            Version = version;
            Description = description;
            DownloadUrl = url;
        }
    }
}
