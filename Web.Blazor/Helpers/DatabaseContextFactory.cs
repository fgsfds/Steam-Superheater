using Common.Interfaces;
using Web.Blazor.Database;

namespace Web.Blazor.Helpers
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
