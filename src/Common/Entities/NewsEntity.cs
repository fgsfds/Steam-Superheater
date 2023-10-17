namespace Common.Entities
{
    public sealed partial class NewsEntity
    {
        /// <summary>
        /// Date of the news article
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// News article text
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// Is newer than the last read version
        /// </summary>
        public bool IsNewer { get; set; }

        /// <summary>
        /// Serializer constructor
        /// </summary>
        private NewsEntity()
        {
        }
    }
}
