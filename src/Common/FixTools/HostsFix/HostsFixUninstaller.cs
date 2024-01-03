using Common.Entities.Fixes.HostsFix;
using Common.Helpers;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Common.FixTools.HostsFix
{
    public sealed class HostsFixUninstaller
    {
        /// <summary>
        /// Uninstall fix: delete files, restore backup
        /// </summary>
        /// <param name="fix">Fix entity</param>
        /// <param name="hostsFilePath">Path to hosts file</param>
        public void UninstallFix(HostsFixEntity fix, string hostsFilePath)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                ThrowHelper.PlatformNotSupportedException(string.Empty);
                return;
            }

            if (fix.InstalledFix is not HostsInstalledFixEntity installedFix)
            {
                ThrowHelper.ArgumentException(nameof(fix.InstalledFix));
                return;
            }

            try
            {
                if (!CommonProperties.IsAdmin)
                {
                    Process.Start(new ProcessStartInfo { FileName = Environment.ProcessPath, UseShellExecute = true, Verb = "runas" });

                    Environment.Exit(0);
                }
            }
            catch
            {
                ThrowHelper.Exception("Superheater needs to be run as admin in order to install hosts fixes");
            }

            List<string> hostsList = [];

            foreach (var line in File.ReadAllLines(hostsFilePath))
            {
                if (!line.EndsWith(installedFix.Guid.ToString()))
                {
                    hostsList.Add(line);
                }
            }

            //converting list to string to prevent adding empty line to the end of the file
            var hostsString = string.Join(Environment.NewLine, hostsList);

            File.WriteAllText(hostsFilePath, hostsString);
        }
    }
}
