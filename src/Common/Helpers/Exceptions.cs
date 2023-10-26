namespace Common.Helpers
{
    public class HashCheckFailedException : Exception
    {
        public HashCheckFailedException(string message)
            : base(message) { }
    }
}
