namespace Common.Entities
{
    public sealed class AppUpdateEntity(
        Version version,
        string description,
        Uri url
        )
    {

        /// <summary>
        /// Release version
        /// </summary>
        public Version Version { get; set; } = version;

        /// <summary>
        /// Release description
        /// </summary>
        public string Description { get; set; } = description;

        /// <summary>
        /// Release download URL
        /// </summary>
        public Uri DownloadUrl { get; set; } = url;
    }
}
