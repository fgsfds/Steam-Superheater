namespace Api.Messages;

public readonly struct ChangeScoreInMessage
{
    public required readonly Guid FixGuid { get; init; }
    public required readonly sbyte Increment { get; init; }
}
