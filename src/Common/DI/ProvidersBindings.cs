using Common.Config;
using Common.Providers;
using Microsoft.Extensions.DependencyInjection;

namespace Common.DI
{
    public static class ProvidersBindings
    {
        public static void Load(ServiceCollection container)
        {
            container.AddScoped<CombinedEntitiesProvider>();
            container.AddScoped<ConfigProvider>();
            container.AddScoped<GamesProvider>();
            container.AddScoped<NewsProvider>();
            container.AddScoped<FixesProvider>();
        }
    }
}
