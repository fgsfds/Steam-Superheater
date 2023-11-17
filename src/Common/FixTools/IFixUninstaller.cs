using Common.Entities;
using Common.Entities.Fixes;

namespace Common.FixTools
{
    public interface IFixUninstaller
    {
        void UninstallFix(GameEntity game, BaseInstalledFixEntity fix, BaseFixEntity fixEntity);
    }
}