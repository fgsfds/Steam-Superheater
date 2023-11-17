namespace Common.Entities.Fixes
{
    public abstract class BaseInstalledFixEntity
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
    }
}