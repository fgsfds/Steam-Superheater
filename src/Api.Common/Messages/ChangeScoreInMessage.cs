using System.Text.Json.Serialization;

namespace Api.Common.Messages;

public sealed class ChangeScoreInMessage
{
    public required Guid FixGuid { get; init; }
    public required sbyte Increment { get; init; }
}


[JsonSerializable(typeof(ChangeScoreInMessage))]
public sealed partial class ChangeScoreInMessageContext : JsonSerializerContext;
