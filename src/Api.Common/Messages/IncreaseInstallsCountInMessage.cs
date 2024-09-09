using System.Text.Json.Serialization;

namespace Api.Common.Messages;

public sealed class IncreaseInstallsCountInMessage
{
    public required Guid FixGuid { get; init; }
}


[JsonSerializable(typeof(IncreaseInstallsCountInMessage))]
public sealed partial class IncreaseInstallsCountInMessageContext : JsonSerializerContext;
