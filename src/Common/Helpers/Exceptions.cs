namespace Common.Helpers
{
    public sealed class HashCheckFailedException(string? message) : Exception(message);

    [Obsolete("Remove in version 1.0")]
    public sealed class BackwardsCompatibilityException(string? message) : Exception(message);
}
