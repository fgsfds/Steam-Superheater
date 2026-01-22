using Common.Axiom.Entities.Fixes;
using Common.Axiom.Entities.Fixes.RegistryFix;
using Common.Axiom.Enums;
using Common.Axiom.Helpers;
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
            throw new PlatformNotSupportedException(string.Empty);
        }

        Guard2.IsOfType<RegistryInstalledFixEntity>(installedFix, out var installedRegFix);

        if (installedFix.DoesRequireAdminRights && !ClientProperties.IsAdmin)
        {
            throw new UnauthorizedAccessException("Superheater needs to be run as admin in order to uninstall this fix");
        }

        foreach (var fix in installedRegFix.Entries)
        {
            var index = fix.Key.IndexOf('\\');
            var baseKey = fix.Key.Substring(0, index);
            var subKey = fix.Key.Substring(index + 1);

            var reg = baseKey switch
            {
                "HKEY_CLASSES_ROOT" => Registry.ClassesRoot,
                "HKEY_CURRENT_USER" => Registry.CurrentUser,
                "HKEY_LOCAL_MACHINE" => Registry.LocalMachine,
                "HKEY_USERS" => Registry.Users,
                _ => throw new NotSupportedException(),
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
                            throw new ArgumentOutOfRangeException($"Unknown type: {fix.ValueType.GetType()}");
                    }
                }
            }
        }
    }
}

