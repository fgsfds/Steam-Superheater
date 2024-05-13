using Web.Server.Database;

namespace Web.Server.Helpers
{
    public class DatabaseContextFactory
    {
        public DatabaseContext Get() => new();
    }
}
