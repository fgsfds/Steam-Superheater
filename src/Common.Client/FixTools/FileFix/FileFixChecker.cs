using Common.Axiom.Entities;
using Common.Axiom.Entities.Fixes;
using Common.Axiom.Entities.Fixes.FileFix;
using Common.Axiom.Helpers;

namespace Common.Client.FixTools.FileFix;

public sealed class FileFixChecker
{
    public async Task<bool> CheckFixHashAsync(GameEntity game, BaseInstalledFixEntity installedFix)
    {
        Guard2.IsOfType<FileInstalledFixEntity>(installedFix, out var installedFileFix);
        ArgumentNullException.ThrowIfNull(installedFileFix.FilesList);

        foreach (var file in installedFileFix.FilesList)
        {
            if (file.Value is null)
            {
                continue;
            }

            var path = Path.Combine(game.InstallDir, file.Key);

            if (!File.Exists(path))
            {
                return false;
            }

            var crc = await Crc32Helper.GetCrc32Async(path, false).ConfigureAwait(false);

            if (crc.Equals(file.Value))
            {
                return false;
            }
        }

        return true;
    }
}

