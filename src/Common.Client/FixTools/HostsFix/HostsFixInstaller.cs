using Common.Entities;
using Common.Entities.Fixes;
using Common.Entities.Fixes.HostsFix;
using CommunityToolkit.Diagnostics;

namespace Common.Client.FixTools.HostsFix;

public sealed class HostsFixInstaller
{
    /// <summary>
    /// Install Hosts fix
    /// </summary>
    /// <param name="game">Game entity</param>
    /// <param name="fix">Fix entity</param>
    /// <param name="hostsFilePath">Path to hosts file</param>
    /// <returns>Installed fix entity</returns>
    public Result<BaseInstalledFixEntity> InstallFix(
        GameEntity game,
        HostsFixEntity fix,
        string hostsFilePath
        )
    {
        if (!OperatingSystem.IsWindows())
        {
            return ThrowHelper.ThrowPlatformNotSupportedException<Result<BaseInstalledFixEntity>>(string.Empty);
        }

        if (!ClientProperties.IsAdmin)
        {
            ThrowHelper.ThrowUnauthorizedAccessException("Superheater needs to be run as admin in order to install hosts fixes");
        }

        var stringToAdd = string.Empty;

        foreach (var line in fix.Entries)
        {
            stringToAdd += Environment.NewLine + line + $" #{fix.Guid}";
        }

        File.AppendAllText(hostsFilePath, stringToAdd);

        return new(
            ResultEnum.Success,
            new HostsInstalledFixEntity()
            {
                GameId = game.Id,
                Guid = fix.Guid,
                Version = fix.Version,
                Entries = [.. fix.Entries]
            },
            "Successfully installed fix");
    }
}

