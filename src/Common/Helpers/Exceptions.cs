namespace Common.Helpers
{
    public sealed class HashCheckFailedException : Exception
    {
        public HashCheckFailedException(string? message)
            : base(message) { }
    }

    public sealed class BackwardsCompatibilityException : Exception
    {
        public BackwardsCompatibilityException(string? message)
            : base(message) { }
    }
}
