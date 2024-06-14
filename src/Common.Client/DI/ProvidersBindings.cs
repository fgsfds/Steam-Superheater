using Common.Client.Config;
using Common.Client.Providers;
using Microsoft.Extensions.DependencyInjection;

namespace Common.Client.DI
{
    public static class ProvidersBindings
    {
        public static void Load(ServiceCollection container)
        {
            container.AddSingleton<CombinedEntitiesProvider>();
            container.AddSingleton<ConfigProvider>();
            container.AddSingleton<GamesProvider>();
            container.AddSingleton<FixesProvider>();
            container.AddSingleton<InstalledFixesProvider>();
        }
    }
}
