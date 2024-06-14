using Common.Client.Config.DbEntities;
using Microsoft.EntityFrameworkCore;

namespace Common.Client.Config;

public sealed class DatabaseContext : DbContext
{
    public DbSet<SettingsDbEntity> Settings { get; set; }
    public DbSet<HiddenTagsDbEntity> HiddenTags { get; set; }
    public DbSet<UpvotesDbEntity> Upvotes { get; set; }

    public DatabaseContext()
    {
        Database.EnsureCreated();
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=config.db");
    }
}
