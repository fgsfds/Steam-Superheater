using Microsoft.EntityFrameworkCore;
using Web.Blazor.DbEntities;
using Web.Blazor.Helpers;

namespace Web.Blazor.Database;

public sealed class DatabaseContext : DbContext
{
    private readonly ServerProperties _properties;

    public DbSet<CommonDbEntity> Common { get; set; }
    public DbSet<InstallsDbEntity> Installs { get; set; }
    public DbSet<ScoresDbEntity> Scores { get; set; }
    public DbSet<ReportsDbEntity> Reports { get; set; }
    public DbSet<NewsDbEntity> News { get; set; }
    public DbSet<GamesDbEntity> Games { get; set; }
    public DbSet<FixesDbEntity> Fixes { get; set; }
    public DbSet<TagsDbEntity> Tags { get; set; }
    public DbSet<TagsListsDbEntity> TagsLists { get; set; }
    public DbSet<DependenciesDbEntity> Dependencies { get; set; }
    public DbSet<FixTypeDbEntity> FixTypes { get; set; }
    public DbSet<HostsFixesDbEntity> HostsFixes { get; set; }
    public DbSet<RegistryValueTypeDbEntity> RegistryValueType { get; set; }
    public DbSet<RegistryFixesDbEntity> RegistryFixes { get; set; }
    public DbSet<FileFixesDbEntity> FileFixes { get; set; }

    public DatabaseContext(ServerProperties properties)
    {
        _properties = properties;

        _ = Database.EnsureCreated();
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (_properties.IsDevMode)
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

