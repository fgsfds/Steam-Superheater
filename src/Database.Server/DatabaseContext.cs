using Database.Server.DbEntities;
using Microsoft.EntityFrameworkCore;

namespace Database.Server;

public sealed class DatabaseContext : DbContext
{
    private readonly bool _isDevMode;

    public DbSet<DatabaseVersionsDbEntity> DatabaseVersions { get; set; }
    public DbSet<DependenciesDbEntity> Dependencies { get; set; }
    public DbSet<FileFixesDbEntity> FileFixes { get; set; }
    public DbSet<FixesDbEntity> Fixes { get; set; }
    public DbSet<GamesDbEntity> Games { get; set; }
    public DbSet<HostsFixesDbEntity> HostsFixes { get; set; }
    public DbSet<NewsDbEntity> News { get; set; }
    public DbSet<RegistryFixesDbEntity> RegistryFixes { get; set; }
    public DbSet<ReportsDbEntity> Reports { get; set; }
    public DbSet<TagsDbEntity> Tags { get; set; }
    public DbSet<TagsListsDbEntity> TagsLists { get; set; }


    public DatabaseContext()
    {
        _isDevMode = true;
        Database.Migrate();
    }

    public DatabaseContext(bool isDevMode)
    {
        _isDevMode = isDevMode;
        Database.Migrate();
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (_isDevMode)
        {
            _ = optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=superheater;Username=postgres;Password=123;Include Error Detail=True");
        }
        else
        {
            var dbip = Environment.GetEnvironmentVariable("DbIp")!;
            var dbport = Environment.GetEnvironmentVariable("DbPort")!;
            var user = Environment.GetEnvironmentVariable("DbUser")!;
            var password = Environment.GetEnvironmentVariable("DbPass")!;
            _ = optionsBuilder.UseNpgsql($"Host={dbip};Port={dbport};Database=superheater;Username={user};Password={password}");
        }
    }
}

