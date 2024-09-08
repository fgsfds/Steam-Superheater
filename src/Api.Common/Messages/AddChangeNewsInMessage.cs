namespace Api.Common.Messages;

public readonly struct AddChangeNewsInMessage
{
    public required readonly DateTime Date { get; init; }
    public required readonly string Content { get; init; }
}
