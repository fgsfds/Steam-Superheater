using System.Text.Json.Serialization;

namespace Api.Axiom.Messages;

public sealed class IncreaseInstallsCountOutMessage
{
    public required int InstallsCount { get; init; }
}


[JsonSerializable(typeof(IncreaseInstallsCountOutMessage))]
public sealed partial class IncreaseInstallsCountOutMessageContext : JsonSerializerContext;
