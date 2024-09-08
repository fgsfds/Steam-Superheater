using Common.Enums;

namespace Api.Common.Messages;

public readonly struct DatabaseVersionsOutMessage
{
    public required readonly Dictionary<DatabaseTableEnum, int> Versions { get; init; }
}
