using CommunityToolkit.Mvvm.ComponentModel;

namespace Common.Entities
{
    public sealed partial class NewsEntity : ObservableObject
    {
        /// <summary>
        /// Version of the news article
        /// </summary>
        public int Version { get; set; }

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
