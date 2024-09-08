using Common.Entities.Fixes;

namespace Api.Common.Messages;

public readonly struct AddFixInMessage
{
    public required readonly int GameId { get; init; }
    public required readonly string GameName { get; init; }
    public required readonly BaseFixEntity Fix {  get; init; }
}
