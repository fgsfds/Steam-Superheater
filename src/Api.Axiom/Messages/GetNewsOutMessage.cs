using System.Text.Json.Serialization;
using Common.Axiom.Entities;

namespace Api.Axiom.Messages;

public sealed class GetNewsOutMessage
{
    public required int Version { get; init; }
    public required List<NewsEntity> News { get; init; }
}


[JsonSerializable(typeof(GetNewsOutMessage))]
public sealed partial class GetNewsOutMessageContext : JsonSerializerContext;
