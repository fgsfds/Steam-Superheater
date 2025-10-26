using System.Text.Json.Serialization;

namespace Api.Axiom.Messages;

public sealed class GetFixesInMessage
{
    public required int TableVersion { get; init; }
    public required Version AppVersion { get; init; }
    public bool DontLog { get; init; }
}


[JsonSerializable(typeof(GetFixesInMessage))]
public sealed partial class GetFixesInMessageContext : JsonSerializerContext;
