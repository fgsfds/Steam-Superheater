using CommunityToolkit.Diagnostics;

namespace Common.Enums;

[Flags]
public enum OSEnum : byte
{
    Windows = 2,
    Linux = 4
}

public static class OSEnumHelper
{
    /// <summary>
    /// Get current OS
    /// </summary>
    public static OSEnum CurrentOSEnum
    {
        get
        {
            if (OperatingSystem.IsWindows())
            {
                return OSEnum.Windows;
            }
            else if (OperatingSystem.IsLinux())
            {
                return OSEnum.Linux;
            }

            return ThrowHelper.ThrowPlatformNotSupportedException<OSEnum>("Error while identifying platform");
        }
    }

    public static OSEnum AddFlag(this OSEnum osenum, OSEnum flag)
    {
        return osenum |= flag;
    }

    public static OSEnum RemoveFlag(this OSEnum osenum, OSEnum flag)
    {
        return osenum &= ~flag;
    }
}

