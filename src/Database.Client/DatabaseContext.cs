#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
using Database.Client.DbEntities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

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
        _ = optionsBuilder.ConfigureWarnings(x =>
            x.Ignore(RelationalEventId.PendingModelChangesWarning)); 
        _ = optionsBuilder.UseSqlite("Data Source=Superheater.db");
    }
}