using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    [Flags]
    public enum OSEnum
    {
        Windows = 2,
        Linux = 4
    }

    public static class OSEnumhelper
    {
        public static OSEnum AddFlag(this OSEnum osenum, OSEnum flag)
        {
            return osenum |= flag;
        }

        public static OSEnum RemoveFlag(this OSEnum osenum, OSEnum flag)
        {
            return osenum &= ~flag;
        }
    }
}
