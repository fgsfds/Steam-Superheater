using System.Text.Json.Serialization;

namespace Api.Axiom.Messages;

public sealed class AddChangeNewsInMessage
{
    public required DateTime Date { get; init; }
    public required string Content { get; init; }
}


[JsonSerializable(typeof(AddChangeNewsInMessage))]
public sealed partial class AddChangeNewsInMessageContext : JsonSerializerContext;