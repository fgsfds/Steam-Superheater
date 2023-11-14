
namespace Common.Entities.Fixes
{
    public interface IInstalledFixEntity
    {
        int GameId { get; init; }
        Guid Guid { get; init; }
        int Version { get; init; }
    }
}