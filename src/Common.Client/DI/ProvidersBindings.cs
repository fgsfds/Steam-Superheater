using Common.Client.Config;
using Common.Client.Providers;
using Microsoft.Extensions.DependencyInjection;

namespace Common.Client.DI
{
    public static class ProvidersBindings
    {
        public static void Load(ServiceCollection container, bool isDesigner)
        {
            if (isDesigner)
            {
                container.AddSingleton<IConfigProvider, ConfigProviderFake>();
            }
            else
            {
                container.AddSingleton<IConfigProvider, ConfigProvider>();
                container.AddSingleton<DatabaseContextFactory>();
            }

            container.AddSingleton<CombinedEntitiesProvider>();
            container.AddSingleton<GamesProvider>();
            container.AddSingleton<FixesProvider>();
            container.AddSingleton<InstalledFixesProvider>();
        }
    }
}
