using Common.Entities.Fixes;
using Common.Entities.Fixes.RegistryFix;
using Common.Entities.Fixes.RegistryFixV2;
using Common.Enums;
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

        if (installedFix.DoesRequireAdminRights && !ClientProperties.IsAdmin)
        {
            ThrowHelper.ThrowUnauthorizedAccessException("Superheater needs to be run as admin in order to uninstall this fix");
        }

        if (installedFix is RegistryInstalledFixEntity installedRegFix)
        {
            var index = installedRegFix.Key.IndexOf("\\");
            var baseKey = installedRegFix.Key.Substring(0, index);
            var subKey = installedRegFix.Key.Substring(index + 1);

            var reg = baseKey switch
            {
                "HKEY_CLASSES_ROOT" => Registry.ClassesRoot,
                "HKEY_CURRENT_USER" => Registry.CurrentUser,
                "HKEY_LOCAL_MACHINE" => Registry.LocalMachine,
                "HKEY_USERS" => Registry.Users,
                _ => ThrowHelper.ThrowNotSupportedException<RegistryKey>(),
            };

            using (var key = reg.OpenSubKey(subKey, true))
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
        else if (installedFix is RegistryInstalledFixV2Entity installedRegFixV2)
        {
            foreach (var fix in installedRegFixV2.Entries)
            {
                var index = fix.Key.IndexOf("\\");
                var baseKey = fix.Key.Substring(0, index);
                var subKey = fix.Key.Substring(index + 1);

                var reg = baseKey switch
                {
                    "HKEY_CLASSES_ROOT" => Registry.ClassesRoot,
                    "HKEY_CURRENT_USER" => Registry.CurrentUser,
                    "HKEY_LOCAL_MACHINE" => Registry.LocalMachine,
                    "HKEY_USERS" => Registry.Users,
                    _ => ThrowHelper.ThrowNotSupportedException<RegistryKey>(),
                };

                using (var key = reg.OpenSubKey(subKey, true))
                {
                    if (key is null)
                    {
                        return;
                    }

                    if (fix.OriginalValue is null)
                    {
                        key.DeleteValue(fix.ValueName);
                    }
                    else
                    {
                        switch (fix.ValueType)
                        {
                            case RegistryValueTypeEnum.String:
                                key.SetValue(fix.ValueName, fix.OriginalValue);
                                break;
                            case RegistryValueTypeEnum.Dword:
                                {
                                    var intValue = int.Parse(fix.OriginalValue);
                                    key.SetValue(fix.ValueName, intValue);
                                    break;
                                }
                            default:
                                ThrowHelper.ThrowArgumentOutOfRangeException($"Unknown type: {fix.ValueType.GetType()}");
                                break;
                        }
                    }
                }
            }
        }
        else
        {
            ThrowHelper.ThrowNotSupportedException();
        }
        
    }
}

