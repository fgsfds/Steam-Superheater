using Common.Entities;
using Common.Entities.Fixes;

namespace Common.FixTools
{
    public interface IFixUninstaller
    {
        void UninstallFix(GameEntity game, IInstalledFixEntity fix, IFixEntity fixEntity);
    }
}