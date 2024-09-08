namespace Api.Common.Messages;

public readonly struct ChangeScoreOutMessage
{
    public required readonly int Score { get; init; }
}
