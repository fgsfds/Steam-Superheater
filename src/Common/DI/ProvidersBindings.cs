using Common.Config;
using Common.Providers;
using Common.Providers.Cached;
using Microsoft.Extensions.DependencyInjection;

namespace Common.DI
{
    public static class ProvidersBindings
    {
        public static void Load(ServiceCollection container)
        {
            container.AddSingleton<CombinedEntitiesProvider>();
            container.AddSingleton<ConfigProvider>();
            container.AddSingleton<GamesProvider>();
            container.AddSingleton<NewsProvider>();
            container.AddSingleton<FixesProvider>();
            container.AddSingleton<InstalledFixesProvider>();
        }
    }
}
