namespace Common.Entities
{
    public class InstalledFixEntity
    {
        /// <summary>
        /// Steam game ID
        /// </summary>
        public int GameId { get; init; }

        /// <summary>
        /// Fix GUID
        /// </summary>
        public Guid Guid { get; init; }

        /// <summary>
        /// Installed version
        /// </summary>
        public int Version { get; init; }

        /// <summary>
        /// Paths to files relative to the game folder
        /// </summary>
        public List<string> FilesList { get; init; }

        public InstalledFixEntity(int id, Guid guid, int version, List<string> list)
        {
            GameId = id;
            Guid = guid;
            Version = version;
            FilesList = list;
        }

        /// <summary>
        /// Serializer constructor
        /// </summary>
        private InstalledFixEntity()
        {
        }
    }
}
