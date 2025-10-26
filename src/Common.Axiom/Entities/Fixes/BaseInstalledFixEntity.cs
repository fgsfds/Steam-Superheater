using System.Text.Json.Serialization;
using Common.Axiom.Entities.Fixes.FileFix;
using Common.Axiom.Entities.Fixes.HostsFix;
using Common.Axiom.Entities.Fixes.RegistryFix;
using Common.Axiom.Enums;

namespace Common.Axiom.Entities.Fixes;

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
    public required string Version { get; init; }

    [JsonIgnore]
    public virtual bool DoesRequireAdminRights => false;
}

[JsonSourceGenerationOptions(
    WriteIndented = true,
    Converters = [typeof(JsonStringEnumConverter<RegistryValueTypeEnum>)])]
[JsonSerializable(typeof(List<BaseInstalledFixEntity>))]
public sealed partial class InstalledFixesListContext : JsonSerializerContext;

