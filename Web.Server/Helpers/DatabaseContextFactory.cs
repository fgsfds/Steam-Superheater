using Common.Interfaces;
using Web.Server.Database;

namespace Web.Server.Helpers
{
    public sealed class DatabaseContextFactory
    {
        private readonly IProperties _properties;

        public DatabaseContextFactory(IProperties properties)
        {
            _properties = properties;
        }

        public DatabaseContext Get() => new(_properties);
    }
}
