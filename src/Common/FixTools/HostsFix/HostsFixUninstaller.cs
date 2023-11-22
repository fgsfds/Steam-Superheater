using Common.Entities.Fixes.HostsFix;
using Common.Helpers;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Common.FixTools.HostsFix
{
    public class HostsFixUninstaller
    {
        /// <summary>
        /// Uninstall fix: delete files, restore backup
        /// </summary>
        /// <param name="fix">Fix entity</param>
        public void UninstallFix(HostsFixEntity fix)
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

            var lines = File.ReadAllLines(Consts.Hosts);
            List<string> newString = new();

            foreach (var line in lines)
            {
                if (!line.EndsWith(installedFix.Guid.ToString()))
                {
                    newString.Add(line);
                }
            }

            File.WriteAllLines(Consts.Hosts, newString);
        }
    }
}
