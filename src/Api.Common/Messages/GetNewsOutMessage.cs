using Common.Entities;
using System.Text.Json.Serialization;

namespace Api.Common.Messages;

public sealed class GetNewsOutMessage
{
    public required int Version { get; init; }
    public required List<NewsEntity> News { get; init; }
}


[JsonSerializable(typeof(GetNewsOutMessage))]
public sealed partial class GetNewsOutMessageContext : JsonSerializerContext;
