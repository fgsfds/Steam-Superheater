using Common.Entities;
using Common.Entities.Fixes;

namespace Common.FixTools
{
    public interface IFixInstaller
    {
        Task<IInstalledFixEntity> InstallFixAsync(GameEntity game, IFixEntity fix, string? variant, bool skipMD5Check);
    }
}