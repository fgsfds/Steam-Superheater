using Common.Entities.Fixes;
using Common.Entities.Fixes.HostsFix;
using Common.Helpers;
using CommunityToolkit.Diagnostics;

namespace Common.Client.FixTools.HostsFix;

public sealed class HostsFixUninstaller
{
    /// <summary>
    /// Uninstall fix: delete files, restore backup
    /// </summary>
    /// <param name="installedFix">Fix entity</param>
    /// <param name="hostsFilePath">Path to hosts file</param>
    public void UninstallFix(
        BaseInstalledFixEntity installedFix,
        string hostsFilePath
        )
    {
        if (!OperatingSystem.IsWindows())
        {
            ThrowHelper.ThrowPlatformNotSupportedException(string.Empty);
            return;
        }

        Guard2.IsOfType<HostsInstalledFixEntity>(installedFix, out var installedHostsFix);

        if (!ClientProperties.IsAdmin)
        {
            ThrowHelper.ThrowUnauthorizedAccessException("Superheater needs to be run as admin in order to uninstall hosts fixes");
        }

        List<string> hostsList = [];

        foreach (var line in File.ReadAllLines(hostsFilePath))
        {
            if (!line.EndsWith(installedHostsFix.Guid.ToString()))
            {
                hostsList.Add(line);
            }
        }

        //converting list to string to prevent adding empty line to the end of the file
        var hostsString = string.Join(Environment.NewLine, hostsList);

        File.WriteAllText(hostsFilePath, hostsString);
    }
}

