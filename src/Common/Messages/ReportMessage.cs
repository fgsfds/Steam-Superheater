namespace Common.Messages
{
    public sealed class ReportMessage
    {
        public required Guid FixGuid { get; set; }
        public required string ReportText { get; set; }
    }
}
