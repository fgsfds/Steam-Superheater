﻿using Common.Entities.Fixes.RegistryFix;
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
        /// <param name="fix">Fix entity</param>
        public void UninstallFix(RegistryFixEntity fix)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                ThrowHelper.PlatformNotSupportedException(string.Empty);
                return;
            }
            if (fix.InstalledFix is not RegistryInstalledFixEntity installedFix)
            {
                ThrowHelper.ArgumentException(nameof(fix.InstalledFix));
                return;
            }

            var newKey = installedFix.Key.Replace("HKEY_CURRENT_USER\\", string.Empty);

            using (var key = Registry.CurrentUser.OpenSubKey(newKey, true))
            {
                if (key is null)
                {
                    return;
                }

                if (installedFix.OriginalValue is null)
                {
                    key.DeleteValue(installedFix.ValueName);
                }
                else
                {
                    switch (fix.ValueType)
                    {
                        case RegistryValueTypeEnum.String:
                            key.SetValue(installedFix.ValueName, installedFix.OriginalValue);
                            break;
                        case RegistryValueTypeEnum.Dword:
                        {
                            var intValue = int.Parse(installedFix.OriginalValue);
                            key.SetValue(installedFix.ValueName, intValue);
                            break;
                        }
                        default:
                            ThrowHelper.ArgumentException($"Unknown type: {fix.ValueType.GetType()}");
                            break;
                    }
                }
            }
        }
    }
}
