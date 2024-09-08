namespace Api.Common.Messages;

public readonly struct ChangeFixStateInMessage
{
    public required readonly Guid FixGuid { get; init; }
    public required readonly bool IsDisabled { get; init; }
}
