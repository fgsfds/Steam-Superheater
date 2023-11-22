using Common.Entities.Fixes.HostsFix;
using Common.Helpers;
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
        }
    }
}
