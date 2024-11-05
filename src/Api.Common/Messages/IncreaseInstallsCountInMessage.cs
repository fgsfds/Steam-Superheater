using System.Text.Json.Serialization;

namespace Api.Common.Messages;

public sealed class IncreaseInstallsCountInMessage
{
    public required Guid FixGuid { get; init; }

    [Obsolete("Make required when 2.2.0 is out of order")]
    public Version AppVersion { get; init; } = new(2, 2, 0);
}


[JsonSerializable(typeof(IncreaseInstallsCountInMessage))]
public sealed partial class IncreaseInstallsCountInMessageContext : JsonSerializerContext;
