using System.ComponentModel;

namespace Common.Enums;

public enum EventTypeEnum : byte
{
    [Description("Get fixes")]
    GetFixes = 1,
    [Description("Install")]
    Install = 2
}
