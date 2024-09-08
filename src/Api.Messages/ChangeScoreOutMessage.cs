namespace Api.Messages;

public readonly struct ChangeScoreOutMessage
{
    public required readonly int Score { get; init; }
}
