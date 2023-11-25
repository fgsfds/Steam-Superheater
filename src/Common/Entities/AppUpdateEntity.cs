namespace Common.Entities
{
    public sealed class AppUpdateEntity
    {
        /// <summary>
        /// Release version
        /// </summary>
        required public Version Version { get; init; }

        /// <summary>
        /// Release description
        /// </summary>
        required public string Description { get; init; }

        /// <summary>
        /// Release download URL
        /// </summary>
        required public Uri DownloadUrl { get; init; }
    }
}
