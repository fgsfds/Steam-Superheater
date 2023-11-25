namespace Common.Entities.Fixes
{
    public abstract class BaseInstalledFixEntity
    {
        /// <summary>
        /// Steam game ID
        /// </summary>
        required public int GameId { get; init; }

        /// <summary>
        /// Fix GUID
        /// </summary>
        required public Guid Guid { get; init; }

        /// <summary>
        /// Installed version
        /// </summary>
        required public int Version { get; set; }
    }
}