namespace Api.Common.Messages;

public readonly struct IncreaseInstallsCountInMessage
{
    public required readonly Guid FixGuid { get; init; }
}
