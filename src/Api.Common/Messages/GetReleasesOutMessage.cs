using Common.Entities;
using Common.Enums;

namespace Api.Common.Messages;

public readonly struct GetReleasesOutMessage
{
    public readonly required Dictionary<OSEnum, AppReleaseEntity> Releases { get; init; }
}
