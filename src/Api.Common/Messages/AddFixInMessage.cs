using Common.Entities.Fixes;
using System.Text.Json.Serialization;

namespace Api.Common.Messages;

public sealed class AddFixInMessage
{
    public required int GameId { get; init; }
    public required string GameName { get; init; }
    public required BaseFixEntity Fix { get; init; }
}


[JsonSerializable(typeof(AddFixInMessage))]
public sealed partial class AddFixInMessageContext : JsonSerializerContext;
