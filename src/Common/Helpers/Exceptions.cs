namespace Common.Helpers
{
    public sealed class HashCheckFailedException(string? message) : Exception(message)
    {
    }

    public sealed class BackwardsCompatibilityException(string? message) : Exception(message)
    {
    }
}
