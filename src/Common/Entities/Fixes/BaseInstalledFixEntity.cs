namespace Common.Entities.Fixes
{
    public abstract class BaseInstalledFixEntity
    {
        /// <summary>
        /// Steam game ID
        /// </summary>
        public required int GameId { get; init; }

        /// <summary>
        /// Fix GUID
        /// </summary>
        public required Guid Guid { get; init; }

        /// <summary>
        /// Installed version
        /// </summary>
        public required int Version { get; init; }
    }
}