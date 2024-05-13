using Microsoft.EntityFrameworkCore;
using Web.Server.DbEntities;

namespace Web.Server.Database
{
    public sealed class DatabaseContext : DbContext
    {
        public DbSet<InstallsDbEntity> Installs { get; set; }
        public DbSet<ScoresDbEntity> Scores { get; set; }
        public DbSet<ReportsDbEntity> Reports { get; set; }
        public DbSet<NewsDbEntity> News { get; set; }

        public DatabaseContext()
        {
            Database.EnsureCreated();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
#if DEBUG
            optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=Superheater;Username=postgres;Password=123");
#else

            string dbip = Environment.GetEnvironmentVariable("DbIp")!;
            string dbport = Environment.GetEnvironmentVariable("DbPort")!;
            string user = Environment.GetEnvironmentVariable("DbUser")!;
            string password = Environment.GetEnvironmentVariable("DbPass")!;
            optionsBuilder.UseNpgsql($"Host={dbip};Port={dbport};Database=superheater;Username={user};Password={password}");
#endif
        }
    }
}
