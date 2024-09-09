using System.ComponentModel;

namespace Common.Enums;

public enum DatabaseTableEnum : byte
{
    [Description("Fixes")]
    Fixes = 1,
    [Description("News")]
    News = 2
}
