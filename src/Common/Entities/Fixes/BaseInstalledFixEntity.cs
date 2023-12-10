using Common.Entities.Fixes.FileFix;
using Common.Entities.Fixes.HostsFix;
using Common.Entities.Fixes.RegistryFix;
using System.Text.Json.Serialization;

namespace Common.Entities.Fixes
{
    [JsonDerivedType(typeof(FileInstalledFixEntity), typeDiscriminator: "FileFix")]
    [JsonDerivedType(typeof(HostsInstalledFixEntity), typeDiscriminator: "HostsFix")]
    [JsonDerivedType(typeof(RegistryInstalledFixEntity), typeDiscriminator: "RegistryFix")]
    public abstract class BaseInstalledFixEntity
    {
        /// <summary>
        /// Steam game ID
        /// </summary>
        public required int GameId { get; init; }

        /// <summary>
        /// Fix GUID
        /// </summary>
        public required Guid Guid { get; init; }

        /// <summary>
        /// Installed version
        /// </summary>
        public required int Version { get; init; }
    }

    [JsonSourceGenerationOptions(WriteIndented = true)]
    [JsonSerializable(typeof(List<BaseInstalledFixEntity>))]
    internal sealed partial class InstalledFixesListContext : JsonSerializerContext { }
}