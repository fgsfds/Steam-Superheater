using ClientCommon.Config;
using ClientCommon.Providers;
using Microsoft.Extensions.DependencyInjection;

namespace ClientCommon.DI
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
