using Common.Interfaces;

namespace Web.Server.Helpers;

public sealed class ServerProperties : IProperties
{
    public bool IsDevMode { get; set; } = false;
}
