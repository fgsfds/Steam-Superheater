namespace Common.Client.Config;

public sealed class DatabaseContextFactory
{
    public DatabaseContext Get() => new();
}
