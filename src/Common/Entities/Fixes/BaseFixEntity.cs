using Common.Entities.Fixes.FileFix;
using Common.Entities.Fixes.HostsFix;
using Common.Entities.Fixes.RegistryFix;
using Common.Entities.Fixes.TextFix;
using Common.Enums;
using System.Text;
using System.Text.Json.Serialization;

namespace Common.Entities.Fixes;

/// <summary>
/// Base fix entity
/// </summary>
[JsonDerivedType(typeof(FileFixEntity), typeDiscriminator: "FileFix")]
[JsonDerivedType(typeof(HostsFixEntity), typeDiscriminator: "HostsFix")]
[JsonDerivedType(typeof(RegistryFixEntity), typeDiscriminator: "RegistryFix")]
[JsonDerivedType(typeof(TextFixEntity), typeDiscriminator: "TextFix")]
public abstract class BaseFixEntity
{
    /// <summary>
    /// Fix title
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Fix GUID
    /// </summary>
    public required Guid Guid { get; init; }

    /// <summary>
    /// Fix version
    /// </summary>
    public required int Version { get; set; }

    /// <summary>
    /// Supported OSes
    /// </summary>
    public required OSEnum SupportedOSes { get; set; }

    /// <summary>
    /// List of fixes GUIDs that are required for this fix
    /// </summary>
    public List<Guid>? Dependencies { get; set; }

    /// <summary>
    /// Fix description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// List of tags
    /// </summary>
    public List<string>? Tags { get; set; }

    /// <summary>
    /// Notes for the fix that are only visible in the editor
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Number of installs
    /// </summary>
    public int? Installs { get; set; }

    /// <summary>
    /// Fix's score
    /// </summary>
    public int? Score { get; set; }

    /// <summary>
    /// Is fix disabled in the database
    /// </summary>
    public bool IsDisabled { get; set; }

    /// <summary>
    /// Is this a test fix
    /// </summary>
    [JsonIgnore]
    public bool IsTestFix { get; set; }

    /// <summary>
    /// Installed fix entity
    /// </summary>
    [JsonIgnore]
    public BaseInstalledFixEntity? InstalledFix { get; set; }

    /// <summary>
    /// Is this fix hidden from the list
    /// </summary>
    [JsonIgnore]
    public bool IsHidden { get; set; }

    /// <summary>
    /// Is fix installed
    /// </summary>
    [JsonIgnore]
    public bool IsInstalled => InstalledFix is not null;

    /// <summary>
    /// Is there a newer version of the fix
    /// </summary>
    [JsonIgnore]
    public virtual bool IsOutdated => InstalledFix is not null && InstalledFix.Version < Version;

    [JsonIgnore]
    public int DependencyLevel { get; set; } = 0;

    [JsonIgnore]
    public string SortedName
    {
        get
        {
            if (DependencyLevel == 0)
            {
                return Name;
            }

            StringBuilder sb = new(Name.Length + 5);

            for (var i = 0; i < DependencyLevel; i++)
            {
                _ = sb.Append("     ");
            }

            _ = sb.Append("⤷ ").Append(Name);

            return sb.ToString();
        }
    }

    public override string ToString() => Name;
}

