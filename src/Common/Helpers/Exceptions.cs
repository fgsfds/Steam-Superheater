namespace Common.Helpers
{
    public class HashCheckFailedException : Exception
    {
        public HashCheckFailedException(string message)
            : base(message) { }
    }

    public class BackwardsCompatibilityException : Exception
    {
        public BackwardsCompatibilityException(string message)
            : base(message) { }
    }
}
