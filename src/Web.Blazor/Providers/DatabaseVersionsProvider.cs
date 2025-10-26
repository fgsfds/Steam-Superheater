using Common.Axiom.Enums;
using Database.Server;

namespace Web.Blazor.Providers;

public sealed class DatabaseVersionsProvider
{
    private readonly DatabaseContextFactory _dbContextFactory;


    public DatabaseVersionsProvider(
        DatabaseContextFactory dbContextFactory
        )
    {
        _dbContextFactory = dbContextFactory;
    }

    public Dictionary<DatabaseTableEnum, int> GetDatabaseVersions()
    {
        Dictionary<DatabaseTableEnum, int> result = [];

        using var dbContext = _dbContextFactory.Get();

        foreach (var table in dbContext.DatabaseVersions)
        {
            result.Add(table.Id, table.Version);
        }

        return result;
    }
}
