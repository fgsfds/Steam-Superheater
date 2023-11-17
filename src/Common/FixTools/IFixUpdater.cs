using Common.Entities;
using Common.Entities.Fixes;

namespace Common.FixTools
{
    public interface IFixUpdater
    {
        Task<BaseInstalledFixEntity> UpdateFixAsync(GameEntity game, BaseFixEntity fix, string? variant, bool skipMD5Check);
    }
}