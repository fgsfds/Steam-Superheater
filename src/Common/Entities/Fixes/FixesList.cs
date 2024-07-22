using Common.Enums;
using System.Text.Json.Serialization;

namespace Common.Entities.Fixes
{
    /// <summary>
    /// Entity containing game information and a list of fixes for it
    /// </summary>
    public sealed class FixesList
    {
        /// <summary>
        /// Steam ID of the game
        /// </summary>
        public required int GameId { get; init; }

        /// <summary>
        /// Game title
        /// </summary>
        public required string GameName { get; init; }

        /// <summary>
        /// List of fixes
        /// </summary>
        public required List<BaseFixEntity> Fixes { get; init; }

        /// <summary>
        /// Does list have any not hidden fixes
        /// </summary>
        [JsonIgnore]
        public bool IsEmpty => Fixes.Count == 0 || !Fixes.Any(static x => !x.IsHidden);
    }

    [JsonSourceGenerationOptions(
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = [typeof(JsonStringEnumConverter<OSEnum>), typeof(JsonStringEnumConverter<RegistryValueTypeEnum>)]
    )]
    [JsonSerializable(typeof(List<FixesList>))]
    public sealed partial class FixesListContext : JsonSerializerContext;
}
