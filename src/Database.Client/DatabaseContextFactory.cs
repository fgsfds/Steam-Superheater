using Microsoft.EntityFrameworkCore;

namespace Database.Client;

public sealed class DatabaseContextFactory
{
    private readonly bool _isFirstStart = true;

    public DatabaseContextFactory()
    {
        if (_isFirstStart)
        {
            using var dbContext = Get();
            dbContext.Database.Migrate();
            _isFirstStart = false;
        }
    }

    public DatabaseContext Get() => new();
}