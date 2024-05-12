namespace Common.Messages
{
    public sealed class RatingMessage
    {
        public Guid FixGuid { get; set; }
        public sbyte Score { get; set; }
    }
}
