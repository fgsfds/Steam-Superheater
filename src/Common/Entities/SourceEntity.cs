namespace Common.Entities;

public sealed class SourceEntity
{
    public required string Name { get; init; }
    public required Uri Url { get; init; }
    public required bool IsEnabled{ get; init; }
}
