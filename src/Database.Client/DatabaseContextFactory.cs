using Microsoft.EntityFrameworkCore;

namespace Database.Client;

/// <summary>
/// Provides database contexts backed by the DI <see cref="IDbContextFactory{TContext}" />.
/// </summary>
public sealed class DatabaseContextFactory
{
    private readonly IDbContextFactory<DatabaseContext> _factory;

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseContextFactory" /> class backed by the DI factory.
    /// </summary>
    /// <param name="factory">The underlying EF Core context factory.</param>
    public DatabaseContextFactory(IDbContextFactory<DatabaseContext> factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Initializes a standalone instance of the <see cref="DatabaseContextFactory" /> class for tooling and tests.
    /// </summary>
    public DatabaseContextFactory()
    {
        var options = new DbContextOptionsBuilder<DatabaseContext>()
            .UseSqlite("Data Source=Superheater.db")
            .Options;

        _factory = new SimpleDbContextFactory(options);

        using var dbContext = _factory.CreateDbContext();
        dbContext.Database.Migrate();
    }


    /// <summary>
    /// Creates a new database context.
    /// </summary>
    /// <returns>The database context.</returns>
    public DatabaseContext Get() => _factory.CreateDbContext();


    /// <summary>
    /// Minimal <see cref="IDbContextFactory{TContext}" /> implementation over a fixed set of options.
    /// </summary>
    private sealed class SimpleDbContextFactory : IDbContextFactory<DatabaseContext>
    {
        private readonly DbContextOptions<DatabaseContext> _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleDbContextFactory" /> class.
        /// </summary>
        /// <param name="options">The context options.</param>
        public SimpleDbContextFactory(DbContextOptions<DatabaseContext> options)
        {
            _options = options;
        }

        /// <inheritdoc />
        public DatabaseContext CreateDbContext() => new(_options);
    }
}
