using Database.Client.DbEntities;
using Microsoft.EntityFrameworkCore;

namespace Database.Client;

public sealed class DatabaseContext : DbContext
{
    public DbSet<SettingsDbEntity> Settings { get; set; }
    public DbSet<HiddenTagsDbEntity> HiddenTags { get; set; }
    public DbSet<UpvotesDbEntity> Upvotes { get; set; }

    public DatabaseContext()
    {
        try
        {
            Database.Migrate();
        }
        catch (Exception ex)
        {
            ConvertOldConfig();
        }
    }

    [Obsolete]
    private void ConvertOldConfig()
    {
        var settings = Settings.ToList();
        var hiddenTags = HiddenTags.ToList();
        var upvotes = Upvotes.ToList();

        Database.EnsureDeleted();
        Database.Migrate();

        Settings.AddRange(settings);
        HiddenTags.AddRange(hiddenTags);
        Upvotes.AddRange(upvotes);

        SaveChanges();
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=config.db");
    }
}
