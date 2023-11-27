using Common.Helpers;
using System.Runtime.InteropServices;

namespace Common.Enums
{
    [Flags]
    public enum OSEnum : byte
    {
        Windows = 2,
        Linux = 4
    }

    public static class OSEnumHelper
    {
        public static OSEnum AddFlag(this OSEnum osenum, OSEnum flag)
        {
            return osenum |= flag;
        }

        public static OSEnum RemoveFlag(this OSEnum osenum, OSEnum flag)
        {
            return osenum &= ~flag;
        }

        /// <summary>
        /// Get current OS
        /// </summary>
        public static OSEnum GetCurrentOSEnum()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return OSEnum.Windows;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return OSEnum.Linux;
            }

            return ThrowHelper.PlatformNotSupportedException<OSEnum>("Error while identifying platform");
        }
    }
}
