using Common.Enums;

namespace Api.Messages;

public readonly struct DatabaseVersionsOutMessage
{
    public required readonly Dictionary<DatabaseTableEnum, int> Versions { get; init; }
}
