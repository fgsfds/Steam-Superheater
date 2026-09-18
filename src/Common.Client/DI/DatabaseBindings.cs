using Database.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Common.Client.DI;

/// <summary>
/// Registers the database services.
/// </summary>
public static class DatabaseBindings
{
    /// <summary>
    /// Registers the database context factory.
    /// </summary>
    /// <param name="container">The service collection.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection WithDatabase(this IServiceCollection container)
    {
        _ = container.AddDbContextFactory<DatabaseContext>(options =>
            options.UseSqlite("Data Source=Superheater.db"));

        _ = container.AddSingleton<DatabaseContextFactory>();

        return container;
    }
}
