using System.Text.Json.Serialization;

namespace Common.Entities
{
    public sealed class AppUpdateEntity
    {
        /// <summary>
        /// Release version
        /// </summary>
        public required Version Version { get; init; }

        /// <summary>
        /// Release description
        /// </summary>
        public required string Description { get; init; }

        /// <summary>
        /// Release download URL
        /// </summary>
        public required Uri DownloadUrl { get; init; }
    }

    [JsonSourceGenerationOptions(WriteIndented = true)]
    [JsonSerializable(typeof(List<AppUpdateEntity>))]
    public sealed partial class AppUpdateEntityContext : JsonSerializerContext;
}
