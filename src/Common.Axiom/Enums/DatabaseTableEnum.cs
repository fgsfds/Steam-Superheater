using System.ComponentModel;

namespace Common.Axiom.Enums;

public enum DatabaseTableEnum : byte
{
    [Description("Fixes")]
    Fixes = 1,
    [Description("News")]
    News = 2
}
