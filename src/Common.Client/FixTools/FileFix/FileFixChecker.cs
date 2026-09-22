using Common.Axiom.Entities;
using Common.Axiom.Entities.Fixes;
using Common.Axiom.Entities.Fixes.FileFix;
using Common.Axiom.Helpers;

namespace Common.Client.FixTools.FileFix;

/// <summary>
/// Verifies installed file fixes against the checksums recorded during installation.
/// </summary>
public sealed class FileFixChecker
{
    /// <summary>
    /// Check that every file of an installed file fix is present and unmodified.
    /// </summary>
    /// <param name="game">Game the fix is installed for.</param>
    /// <param name="installedFix">Installed fix state.</param>
    /// <returns><see langword="true"/> when every file with a recorded checksum matches; otherwise <see langword="false"/>.</returns>
    public async Task<bool> CheckFixHashAsync(GameEntity game, BaseInstalledFixEntity installedFix)
    {
        if (installedFix is not FileInstalledFixEntity installedFileFix)
        {
            throw new ArgumentException("Installed fix is not a file fix.", nameof(installedFix));
        }
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

            var crc = await Crc32Helper.GetCrc32Async(path).ConfigureAwait(false);

            if (crc != file.Value)
            {
                return false;
            }
        }

        return true;
    }
}
