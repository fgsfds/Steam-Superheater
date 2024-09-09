using Common.Enums;
using Database.Server;

namespace Web.Blazor.Providers;

public sealed class DatabaseVersionsProvider
{
    private readonly DatabaseContextFactory _dbContextFactory;
    private readonly HttpClient _httpClient;
    private readonly ILogger<FixesProvider> _logger;


    public DatabaseVersionsProvider(
        ILogger<FixesProvider> logger,
        HttpClient httpClient,
        DatabaseContextFactory dbContextFactory
        )
    {
        _httpClient = httpClient;
        _logger = logger;
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

