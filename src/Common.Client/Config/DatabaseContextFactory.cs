namespace Common.Client.Config;

public class DatabaseContextFactory
{
    public DatabaseContext Get() => new();
}
