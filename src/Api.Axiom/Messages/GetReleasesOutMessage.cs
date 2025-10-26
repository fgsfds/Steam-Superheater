using System.Text.Json.Serialization;
using Common.Axiom.Entities;
using Common.Axiom.Enums;

namespace Api.Axiom.Messages;

public sealed class GetReleasesOutMessage
{
    public required Dictionary<OSEnum, AppReleaseEntity> Releases { get; init; }
}


[JsonSerializable(typeof(GetReleasesOutMessage))]
public sealed partial class GetReleasesOutMessageContext : JsonSerializerContext;
