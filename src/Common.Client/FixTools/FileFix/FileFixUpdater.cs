using Common.Axiom;
using Common.Axiom.Entities;
using Common.Axiom.Entities.Fixes;
using Common.Axiom.Entities.Fixes.FileFix;

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
        ArgumentNullException.ThrowIfNull(fileFix.InstalledFix);

        var validation = await _fixInstaller.ValidateArchiveAsync(game, fileFix, variant, skipMD5Check, cancellationToken).ConfigureAwait(false);
        if (!validation.IsSuccess)
        {
            return new(validation.ResultEnum, null, validation.Message);
        }

        _fixUninstaller.UninstallFix(game, fileFix.InstalledFix);

        var result = await _fixInstaller.InstallFixAsync(game, fileFix, variant, skipMD5Check, cancellationToken).ConfigureAwait(false);

        return result;
    }
}

