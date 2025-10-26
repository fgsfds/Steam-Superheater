using System.Text.Json.Serialization;
using Common.Axiom.Entities.Fixes;

namespace Api.Axiom.Messages;

public sealed class AddFixInMessage
{
    public required int GameId { get; init; }
    public required string GameName { get; init; }
    public required BaseFixEntity Fix { get; init; }
}


[JsonSerializable(typeof(AddFixInMessage))]
public sealed partial class AddFixInMessageContext : JsonSerializerContext;
