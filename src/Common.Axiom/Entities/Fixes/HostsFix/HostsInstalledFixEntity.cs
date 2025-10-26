using System.Text.Json.Serialization;

namespace Common.Axiom.Entities.Fixes.HostsFix;

public sealed class HostsInstalledFixEntity : BaseInstalledFixEntity
{
    /// <summary>
    /// List of entries that were added to the hosts file
    /// </summary>
    public required List<string> Entries { get; init; }

    [JsonIgnore]
    public override bool DoesRequireAdminRights => true;
}

