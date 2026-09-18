using Common.Axiom;
using Common.Axiom.Providers;
using Common.Client.Config;
using Common.Client.Providers;
using Common.Client.Providers.Fakes;
using Common.Client.Providers.Interfaces;
using Database.Client;
using Microsoft.Extensions.DependencyInjection;

namespace Common.Client.DI;

/// <summary>
/// Registers the client providers.
/// </summary>
public static class ProvidersBindings
{
    /// <summary>
    /// Registers the client providers, using fakes when running in the designer.
    /// </summary>
    /// <param name="container">The service collection.</param>
    /// <param name="isDesigner">Whether the app is running in the Avalonia designer.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection WithProviders(this IServiceCollection container, bool isDesigner)
    {
        if (isDesigner)
        {
            _ = container.AddSingleton<IConfigProvider, ConfigProviderFake>();
            _ = container.AddSingleton<IFixesProvider, FixesProviderFake>();
            _ = container.AddSingleton<INewsProvider, NewsProviderFake>();
            _ = container.AddSingleton<IGamesProvider, GamesProviderFake>();
            _ = container.AddSingleton<IInstalledFixesProvider, InstalledFixesProviderFake>();
            _ = container.AddSingleton<DatabaseContextFactory>();

            return container;
        }

        _ = container.AddSingleton<IConfigProvider, ConfigProvider>();
        _ = container.AddSingleton<IFixesProvider, FixesProvider>();
        _ = container.AddSingleton<INewsProvider, NewsProvider>();
        _ = container.AddSingleton<IGamesProvider, GamesProvider>();
        _ = container.AddSingleton<IInstalledFixesProvider, InstalledFixesProvider>();
        _ = container.AddSingleton<DatabaseContextFactory>();
        _ = container.AddSingleton<AppReleasesProvider>();

        return container;
    }
}
