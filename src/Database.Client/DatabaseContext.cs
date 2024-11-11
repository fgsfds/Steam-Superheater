using Database.Client.DbEntities;
using Microsoft.EntityFrameworkCore;

namespace Database.Client;

public sealed class DatabaseContext : DbContext
{
    public DbSet<CacheDbEntity> Cache { get; set; }
    public DbSet<SettingsDbEntity> Settings { get; set; }
    public DbSet<HiddenTagsDbEntity> HiddenTags { get; set; }
    public DbSet<UpvotesDbEntity> Upvotes { get; set; }

    public DatabaseContext()
    {
        Database.Migrate();
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        _ = optionsBuilder.UseSqlite("Data Source=Superheater.db");
    }
}