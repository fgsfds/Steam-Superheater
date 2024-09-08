namespace Api.Common.Messages;

public readonly struct ReportFixInMessage
{
    public required readonly Guid FixGuid{ get; init; }
    public required readonly string Text { get; init; }
}
