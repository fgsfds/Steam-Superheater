using System.Text.Json.Serialization;

namespace Api.Axiom.Messages;

public sealed class ReportFixInMessage
{
    public required Guid FixGuid { get; init; }
    public required string Text { get; init; }
}


[JsonSerializable(typeof(ReportFixInMessage))]
public sealed partial class ReportFixInMessageContext : JsonSerializerContext;
