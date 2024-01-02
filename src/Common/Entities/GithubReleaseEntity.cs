#pragma warning disable IDE1006 // Naming Styles
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

using System.Text.Json.Serialization;

namespace Common.Entities
{
    /// <summary>
    /// Class is auto generated from the json response
    /// </summary>
    public sealed class GitHubRelease
    {
        public string tag_name { get; set; }

        public bool draft { get; set; }

        public bool prerelease { get; set; }

        public Asset[] assets { get; set; }

        public string body { get; set; }
    }

    public sealed class Asset
    {
        public string name { get; set; }

        public string browser_download_url { get; set; }
    }

    [JsonSerializable(typeof(List<GitHubRelease>))]
    internal sealed partial class GitHubReleaseContext : JsonSerializerContext { }
}
#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
