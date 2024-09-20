using System.Text.Json.Serialization;

namespace Api.Common.Messages;

public sealed class GetFixesStatsOutMessage
{
    public required Dictionary<Guid, int> Scores { get; init; }

    public required Dictionary<Guid, int> Installs { get; init; }
}


[JsonSerializable(typeof(GetFixesStatsOutMessage))]
public sealed partial class GetFixesStatsOutMessageContext : JsonSerializerContext;
