using System.ComponentModel;

namespace Common.Axiom.Enums;

public enum FixTypeEnum : byte
{
    [Description("File fix")]
    FileFix = 1,
    [Description("Registry fix")]
    RegistryFix = 2,
    [Description("Hosts fix")]
    HostsFix = 3,
    [Description("Text fix")]
    TextFix = 4
}
