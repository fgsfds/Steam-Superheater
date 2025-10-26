using System.Text.Json.Serialization;
using Common.Axiom.Enums;

namespace Api.Axiom.Messages;

public sealed class DatabaseVersionsOutMessage
{
    public required Dictionary<DatabaseTableEnum, int> Versions { get; init; }
}


[JsonSerializable(typeof(DatabaseVersionsOutMessage))]
public sealed partial class DatabaseVersionsOutMessageContext : JsonSerializerContext;
