using System.Runtime.InteropServices;

namespace Common
{
    [Flags]
    public enum OSEnum
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

        public static OSEnum GetCurrentOS()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return OSEnum.Windows;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return OSEnum.Linux;
            }

            throw new NotImplementedException();
        }
    }
}
