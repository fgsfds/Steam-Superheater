using Common.Entities.Fixes;
using System.Text.Json.Serialization;

namespace Api.Common.Messages;

public sealed class GetFixesOutMessage
{
    public required int Version { get; init; }
    public required List<FixesList> Fixes { get; init; }
}


[JsonSerializable(typeof(GetFixesOutMessage))]
public sealed partial class GetFixesOutMessageContext : JsonSerializerContext;
