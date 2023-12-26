#pragma warning disable IDE1006 // Naming Styles

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
