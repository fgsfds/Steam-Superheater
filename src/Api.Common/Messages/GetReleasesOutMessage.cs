using Common.Entities;
using Common.Enums;
using System.Text.Json.Serialization;

namespace Api.Common.Messages;

public sealed class GetReleasesOutMessage
{
    public required Dictionary<OSEnum, AppReleaseEntity> Releases { get; init; }
}


[JsonSerializable(typeof(GetReleasesOutMessage))]
public sealed partial class GetReleasesOutMessageContext : JsonSerializerContext;
