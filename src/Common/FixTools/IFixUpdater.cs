using Common.Entities;
using Common.Entities.Fixes;

namespace Common.FixTools
{
    public interface IFixUpdater
    {
        Task<IInstalledFixEntity> UpdateFixAsync(GameEntity game, IFixEntity fix, string? variant, bool skipMD5Check);
    }
}