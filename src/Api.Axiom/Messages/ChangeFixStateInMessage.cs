using System.Text.Json.Serialization;

namespace Api.Axiom.Messages;

public sealed class ChangeFixStateInMessage
{
    public required Guid FixGuid { get; init; }
    public required bool IsDisabled { get; init; }
}


[JsonSerializable(typeof(ChangeFixStateInMessage))]
public sealed partial class ChangeFixStateInMessageContext : JsonSerializerContext;
