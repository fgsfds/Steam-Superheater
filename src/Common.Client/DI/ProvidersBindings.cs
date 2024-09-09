using Common.Client.Config;
using Common.Client.Providers;
using Database.Client;
using Microsoft.Extensions.DependencyInjection;

namespace Common.Client.DI;

public static class ProvidersBindings
{
    public static void Load(ServiceCollection container, bool isDesigner)
    {
        if (isDesigner)
        {
            _ = container.AddSingleton<IConfigProvider, ConfigProviderFake>();
        }
        else
        {
            _ = container.AddSingleton<IConfigProvider, ConfigProvider>();
        }

        _ = container.AddSingleton<DatabaseContextFactory>();
        _ = container.AddSingleton<GamesProvider>();
        _ = container.AddSingleton<FixesProvider>();
        _ = container.AddSingleton<InstalledFixesProvider>();
        _ = container.AddSingleton<NewsProvider>();
    }
}

