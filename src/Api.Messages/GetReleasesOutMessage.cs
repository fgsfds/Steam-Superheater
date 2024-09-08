using Common.Entities;

namespace Api.Messages;

public readonly struct GetReleasesOutMessage
{
    public required AppReleaseEntity Windows { get; init; }
    public required AppReleaseEntity Linux { get; init; }
}
