namespace Common.Entities
{
    public sealed class UpdateEntity
    {
        public UpdateEntity(
            Version version,
            string description,
            Uri url)
        {
            Version = version;
            Description = description;
            DownloadUrl = url;
        }

        public Version Version { get; set; }

        public string Description { get; set; }

        public Uri DownloadUrl { get; set; }
    }
}
