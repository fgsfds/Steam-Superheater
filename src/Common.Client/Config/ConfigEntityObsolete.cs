using Common.Enums;
using System.Text.Json.Serialization;

namespace Common.Client.Config
{
    [Obsolete]
    public sealed class ConfigEntityObsolete
    {
        public bool DeleteZipsAfterInstall { get; set; }

        public bool OpenConfigAfterInstall { get; set; }

        public bool UseLocalApiAndRepo { get; set; }

        public bool ShowUninstalledGames { get; set; }

        public bool ShowUnsupportedFixes { get; set; }

        public string LocalRepoPath { get; set; }

        public string ApiPassword { get; set; }

        public ThemeEnum Theme { get; set; }

        public DateTime LastReadNewsDate { get; set; }

        public List<string> HiddenTags { get; set; }

        public Dictionary<Guid, bool> Upvotes { get; set; }
    }

    [JsonSourceGenerationOptions(
        WriteIndented = true,
        Converters = [typeof(JsonStringEnumConverter<ThemeEnum>)]
    )]
    [JsonSerializable(typeof(ConfigEntityObsolete))]
    internal sealed partial class ConfigEntityContext : JsonSerializerContext;
}
