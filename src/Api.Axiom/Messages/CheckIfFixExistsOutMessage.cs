using System.Text.Json.Serialization;

namespace Api.Axiom.Messages;

public sealed class CheckIfFixExistsOutMessage
{
    public required string CurrentVersion { get; init; }
}


[JsonSerializable(typeof(CheckIfFixExistsOutMessage))]
public sealed partial class CheckIfFixExistsOutMessageContext : JsonSerializerContext;