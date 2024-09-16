using Common.Entities;
using Common.Entities.Fixes;
using Common.Entities.Fixes.RegistryFixV2;
using CommunityToolkit.Diagnostics;

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
        RegistryFixV2Entity regFix
        )
    {
        Guard.IsNotNull(regFix.InstalledFix);

        _fixUninstaller.UninstallFix(regFix.InstalledFix);

        var result = _fixInstaller.InstallFix(game, regFix);

        return result;
    }
}

