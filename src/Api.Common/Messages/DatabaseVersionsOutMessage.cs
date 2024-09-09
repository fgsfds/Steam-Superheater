using Common.Enums;
using System.Text.Json.Serialization;

namespace Api.Common.Messages;

public sealed class DatabaseVersionsOutMessage
{
    public required Dictionary<DatabaseTableEnum, int> Versions { get; init; }
}


[JsonSerializable(typeof(DatabaseVersionsOutMessage))]
public sealed partial class DatabaseVersionsOutMessageContext : JsonSerializerContext;
