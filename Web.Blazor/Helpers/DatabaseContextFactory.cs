using Web.Blazor.Database;

namespace Web.Blazor.Helpers;

public sealed class DatabaseContextFactory
{
    private readonly ServerProperties _properties;

    public DatabaseContextFactory(ServerProperties properties)
    {
        _properties = properties;
    }

    public DatabaseContext Get() => new(_properties);
}

