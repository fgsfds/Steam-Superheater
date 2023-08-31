using CommunityToolkit.Mvvm.ComponentModel;

namespace SteamFDCommon.Entities
{
    public partial class NewsEntity : ObservableObject
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
        [ObservableProperty]
        private bool _isNewer;

        /// <summary>
        /// Serializer constructor
        /// </summary>
        private NewsEntity()
        {
        }
    }
}
