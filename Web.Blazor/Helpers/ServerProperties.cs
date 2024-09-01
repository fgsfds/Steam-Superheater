using Common.Interfaces;

namespace Web.Blazor.Helpers;

public sealed class ServerProperties : IProperties
{
    public bool IsDevMode { get; set; } = false;
}