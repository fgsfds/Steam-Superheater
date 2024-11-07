using System.Text.Json.Serialization;

namespace Api.Common.Messages;

public sealed class IncreaseInstallsCountInMessage
{
    public required Guid FixGuid { get; init; }

    [Obsolete("Make required when there's no versions <2.2.0")]
    public Version? AppVersion { get; set; }

    public bool DontLog { get; init; }
}


[JsonSerializable(typeof(IncreaseInstallsCountInMessage))]
public sealed partial class IncreaseInstallsCountInMessageContext : JsonSerializerContext;
