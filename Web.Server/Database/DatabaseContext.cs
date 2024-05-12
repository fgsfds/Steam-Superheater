using Microsoft.EntityFrameworkCore;
using Web.Server.DbEntities;

namespace Web.Server.Database
{
    public sealed class DatabaseContext : DbContext
    {
        public DbSet<InstallsEntity> Downloads { get; set; }
        public DbSet<ScoreEntity> Rating { get; set; }

        public DatabaseContext()
        {
            Database.EnsureCreated();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
#if DEBUG
            optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=Superheater;Username=postgres;Password=123");
#else
            optionsBuilder.UseNpgsql("Host=192.168.0.4;Port=5432;Database=superheater;Username=gen_user;Password=ndi\\2k4OyZZ$9");
#endif
        }
    }
}
