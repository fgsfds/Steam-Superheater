﻿using System.Text.Json.Serialization;

namespace Api.Common.Messages;

public sealed class ChangeScoreOutMessage
{
    public required int Score { get; init; }
}


[JsonSerializable(typeof(ChangeScoreOutMessage))]
public sealed partial class ChangeScoreOutMessageContext : JsonSerializerContext;
