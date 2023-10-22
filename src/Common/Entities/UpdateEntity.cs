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

        /// <summary>
        /// Release version
        /// </summary>
        public Version Version { get; set; }

        /// <summary>
        /// Release description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Release download URL
        /// </summary>
        public Uri DownloadUrl { get; set; }
    }
}
