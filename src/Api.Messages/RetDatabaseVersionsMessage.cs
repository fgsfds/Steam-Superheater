using Common.Enums;

namespace Api.Messages;

public readonly struct RetDatabaseVersionsMessage
{
    public required readonly Dictionary<DatabaseTableEnum, int> Versions { get; init; }
}
