namespace Database.Client;

public sealed class DatabaseContextFactory
{
    public DatabaseContext Get() => new();
}
