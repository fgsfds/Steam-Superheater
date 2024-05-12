using Microsoft.EntityFrameworkCore;
using Web.Server.DbEntities;

namespace Web.Server.Database
{
    public sealed class DatabaseContext : DbContext
    {
        public DbSet<InstallsEntity> Installs { get; set; }
        public DbSet<ScoresEntity> Scores { get; set; }
        public DbSet<ReportsEntity> Reports { get; set; }

        public DatabaseContext()
        {
            Database.EnsureCreated();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
#if DEBUG
            optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=Superheater;Username=postgres;Password=123");
#else

            string user = Environment.GetEnvironmentVariable("DbUser")!;
            string password = Environment.GetEnvironmentVariable("DbPass")!;
            optionsBuilder.UseNpgsql($"Host=192.168.0.4;Port=5432;Database=superheater;Username={user};Password={password}");
#endif
        }
    }
}
