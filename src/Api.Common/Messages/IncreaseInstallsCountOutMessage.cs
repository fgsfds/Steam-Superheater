namespace Api.Common.Messages;

public readonly struct IncreaseInstallsCountOutMessage
{
    public required readonly int InstallsCount { get; init; }
}
