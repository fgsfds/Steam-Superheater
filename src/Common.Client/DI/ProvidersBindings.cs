using Common.Client.Config;
using Common.Client.Providers;
using Common.Client.Providers.Fakes;
using Common.Client.Providers.Interfaces;
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
            _ = container.AddSingleton<IFixesProvider, FixesProviderFake>();
            _ = container.AddSingleton<INewsProvider, NewsProviderFake>();
            _ = container.AddSingleton<IGamesProvider, GamesProviderFake>();
            _ = container.AddSingleton<IInstalledFixesProvider, InstalledFixesProviderFake>();
            _ = container.AddSingleton<DatabaseContextFactory>();

            return;
        }

        _ = container.AddSingleton<IConfigProvider, ConfigProvider>();
        _ = container.AddSingleton<IFixesProvider, FixesProvider>();
        _ = container.AddSingleton<INewsProvider, NewsProvider>();
        _ = container.AddSingleton<IGamesProvider, GamesProvider>();
        _ = container.AddSingleton<IInstalledFixesProvider, InstalledFixesProvider>();
        _ = container.AddSingleton<DatabaseContextFactory>();
    }
}

