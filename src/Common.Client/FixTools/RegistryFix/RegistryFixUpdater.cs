using Common.Axiom;
using Common.Axiom.Entities;
using Common.Axiom.Entities.Fixes;
using Common.Axiom.Entities.Fixes.RegistryFix;

namespace Common.Client.FixTools.RegistryFix;

public sealed class RegistryFixUpdater
{
    private readonly RegistryFixInstaller _fixInstaller;
    private readonly RegistryFixUninstaller _fixUninstaller;


    public RegistryFixUpdater(
        RegistryFixInstaller fixInstaller,
        RegistryFixUninstaller fixUninstaller
        )
    {
        _fixInstaller = fixInstaller;
        _fixUninstaller = fixUninstaller;
    }


    public Result<BaseInstalledFixEntity> UpdateFix(
        GameEntity game,
        RegistryFixEntity regFix
        )
    {
        ArgumentNullException.ThrowIfNull(regFix.InstalledFix);

        _fixUninstaller.UninstallFix(regFix.InstalledFix);

        var result = _fixInstaller.InstallFix(game, regFix);

        return result;
    }
}

