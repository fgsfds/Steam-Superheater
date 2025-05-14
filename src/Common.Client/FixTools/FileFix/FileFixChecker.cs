using Common.Entities;
using Common.Entities.Fixes;
using Common.Entities.Fixes.FileFix;
using Common.Helpers;
using CommunityToolkit.Diagnostics;

namespace Common.Client.FixTools.FileFix;

public sealed class FileFixChecker
{
    public async Task<bool> CheckFixHashAsync(GameEntity game, BaseInstalledFixEntity installedFix)
    {
        Guard2.IsOfType<FileInstalledFixEntity>(installedFix, out var installedFileFix);
        Guard.IsNotNull(installedFileFix.FilesList);

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

