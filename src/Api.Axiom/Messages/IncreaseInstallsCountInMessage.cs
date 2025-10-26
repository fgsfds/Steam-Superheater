using System.Text.Json.Serialization;

namespace Api.Axiom.Messages;

public sealed class IncreaseInstallsCountInMessage
{
    public required Guid FixGuid { get; init; }

    public required Version AppVersion { get; init; }
}


[JsonSerializable(typeof(IncreaseInstallsCountInMessage))]
public sealed partial class IncreaseInstallsCountInMessageContext : JsonSerializerContext;
