using System.ComponentModel;

namespace Common.Enums;

public enum RegistryValueTypeEnum : byte
{
    [Description("String")]
    String = 1,
    [Description("Dword")]
    Dword = 2
}
