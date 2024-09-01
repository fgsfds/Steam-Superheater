using Common.Entities;
using Common.Entities.Fixes;
using Common.Entities.Fixes.FileFix;
using CommunityToolkit.Diagnostics;

namespace Common.Client.FixTools.FileFix;

public sealed class FileFixUpdater
{
    private readonly FileFixInstaller _fixInstaller;
    private readonly FileFixUninstaller _fixUninstaller;


    public FileFixUpdater(
        FileFixInstaller fixInstaller,
        FileFixUninstaller fixUninstaller
        )
    {
        _fixInstaller = fixInstaller;
        _fixUninstaller = fixUninstaller;
    }


    public async Task<Result<BaseInstalledFixEntity>> UpdateFixAsync(
        GameEntity game,
        FileFixEntity fileFix,
        string? variant,
        bool skipMD5Check,
        CancellationToken cancellationToken
        )
    {
        Guard.IsNotNull(fileFix.InstalledFix);

        _fixUninstaller.UninstallFix(game, fileFix.InstalledFix);

        var result = await _fixInstaller.InstallFixAsync(game, fileFix, variant, skipMD5Check, cancellationToken).ConfigureAwait(false);

        return result;
    }
}

