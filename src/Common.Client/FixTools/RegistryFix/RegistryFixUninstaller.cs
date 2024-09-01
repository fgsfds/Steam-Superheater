using Common.Entities.Fixes;
using Common.Entities.Fixes.RegistryFix;
using Common.Enums;
using Common.Helpers;
using CommunityToolkit.Diagnostics;
using Microsoft.Win32;

namespace Common.Client.FixTools.RegistryFix;

public sealed class RegistryFixUninstaller
{
    /// <summary>
    /// Uninstall fix: delete files, restore backup
    /// </summary>
    /// <param name="installedFix">Fix entity</param>
    public void UninstallFix(BaseInstalledFixEntity installedFix)
    {
        if (!OperatingSystem.IsWindows())
        {
            ThrowHelper.ThrowPlatformNotSupportedException(string.Empty);
            return;
        }

        Guard2.IsOfType<RegistryInstalledFixEntity>(installedFix, out var installedRegFix);

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
                        ThrowHelper.ThrowArgumentOutOfRangeException($"Unknown type: {installedRegFix.ValueType.GetType()}");
                        break;
                }
            }
        }
    }
}

