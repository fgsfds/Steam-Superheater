namespace Api.Messages;

public readonly struct IncreaseInstallsCountInMessage
{
    public required readonly Guid FixGuid { get; init; }
}
