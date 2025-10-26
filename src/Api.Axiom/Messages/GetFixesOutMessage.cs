using System.Text.Json.Serialization;
using Common.Axiom.Entities.Fixes;

namespace Api.Axiom.Messages;

public sealed class GetFixesOutMessage
{
    public required int Version { get; init; }
    public required List<FixesList> Fixes { get; init; }
}


[JsonSerializable(typeof(GetFixesOutMessage))]
public sealed partial class GetFixesOutMessageContext : JsonSerializerContext;
