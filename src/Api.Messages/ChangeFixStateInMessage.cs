namespace Api.Messages;

public readonly struct ChangeFixStateInMessage
{
    public required readonly Guid FixGuid { get; init; }
    public required readonly bool IsDisabled { get; init; }
}
