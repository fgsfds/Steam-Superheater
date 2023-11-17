using Common.Entities;
using Common.Entities.Fixes;

namespace Common.FixTools
{
    public interface IFixInstaller
    {
        Task<BaseInstalledFixEntity> InstallFixAsync(GameEntity game, BaseFixEntity fix, string? variant, bool skipMD5Check);
    }
}