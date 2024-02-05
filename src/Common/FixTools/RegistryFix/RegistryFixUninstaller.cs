using Common.Entities.Fixes;
using Common.Entities.Fixes.RegistryFix;
using Common.Enums;
using Common.Helpers;
using Microsoft.Win32;
using System.Runtime.InteropServices;

namespace Common.FixTools.RegistryFix
{
    public sealed class RegistryFixUninstaller
    {
        /// <summary>
        /// Uninstall fix: delete files, restore backup
        /// </summary>
        /// <param name="installedFix">Fix entity</param>
        public void UninstallFix(BaseInstalledFixEntity installedFix)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                ThrowHelper.PlatformNotSupportedException(string.Empty);
                return;
            }
            if (installedFix is not RegistryInstalledFixEntity installedRegFix)
            {
                ThrowHelper.ArgumentException(nameof(installedFix));
                return;
            }

            var newKey = installedRegFix.Key.Replace("HKEY_CURRENT_USER\\", string.Empty);

            using (var key = Registry.CurrentUser.OpenSubKey(newKey, true))
            {
                if (key is null)
                {
                    return;
                }

                if (installedRegFix.OriginalValue is null)
                {
                    key.DeleteValue(installedRegFix.ValueName);
                }
                else
                {
                    switch (installedRegFix.ValueType)
                    {
                        case RegistryValueTypeEnum.String:
                            key.SetValue(installedRegFix.ValueName, installedRegFix.OriginalValue);
                            break;
                        case RegistryValueTypeEnum.Dword:
                        {
                            var intValue = int.Parse(installedRegFix.OriginalValue);
                            key.SetValue(installedRegFix.ValueName, intValue);
                            break;
                        }
                        default:
                            ThrowHelper.ArgumentException($"Unknown type: {installedRegFix.ValueType.GetType()}");
                            break;
                    }
                }
            }
        }
    }
}
