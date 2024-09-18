using Common.Entities.Fixes.FileFix;
using Common.Entities.Fixes.HostsFix;
using Common.Entities.Fixes.RegistryFix;
using Common.Entities.Fixes.RegistryFixV2;
using Common.Enums;
using System.Text.Json.Serialization;

namespace Common.Entities.Fixes;

[JsonDerivedType(typeof(FileInstalledFixEntity), typeDiscriminator: "FileFix")]
[JsonDerivedType(typeof(HostsInstalledFixEntity), typeDiscriminator: "HostsFix")]
[JsonDerivedType(typeof(RegistryInstalledFixEntity), typeDiscriminator: "RegistryFix")]
[JsonDerivedType(typeof(RegistryInstalledFixV2Entity), typeDiscriminator: "RegistryFixV2")]
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
    [Obsolete("Remove later")]
    public required int Version { get; init; }

    /// <summary>
    /// Installed version
    /// </summary>
    [Obsolete("Make required later")]
    public string? VersionStr { get; init; }
}

[JsonSourceGenerationOptions(
    WriteIndented = true,
    Converters = [typeof(JsonStringEnumConverter<RegistryValueTypeEnum>)])]
[JsonSerializable(typeof(List<BaseInstalledFixEntity>))]
public sealed partial class InstalledFixesListContext : JsonSerializerContext;

