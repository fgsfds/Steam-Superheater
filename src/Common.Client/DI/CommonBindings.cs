using Common.Axiom;
using Common.Client.FilesTools;
using Common.Client.FilesTools.Interfaces;
using Common.Client.FixTools;
using Common.Client.FixTools.FileFix;
using Common.Client.FixTools.HostsFix;
using Common.Client.FixTools.RegistryFix;
using Common.Client.Providers;
using Microsoft.Extensions.DependencyInjection;

namespace Common.Client.DI;

/// <summary>
/// Registers the common client services.
/// </summary>
public static class CommonBindings
{
    /// <summary>
    /// Registers the core client services, fix tools and HTTP client.
    /// </summary>
    /// <param name="container">The service collection.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection WithCommon(this IServiceCollection container)
    {
        _ = container.AddTransient<AppUpdateInstaller>();

        _ = container.AddTransient<FileFixInstaller>();
        _ = container.AddTransient<FileFixUpdater>();
        _ = container.AddTransient<FileFixUninstaller>();
        _ = container.AddTransient<FileFixChecker>();

        _ = container.AddTransient<RegistryFixInstaller>();
        _ = container.AddTransient<RegistryFixUpdater>();
        _ = container.AddTransient<RegistryFixUninstaller>();

        _ = container.AddTransient<HostsFixInstaller>();
        _ = container.AddTransient<HostsFixUpdater>();
        _ = container.AddTransient<HostsFixUninstaller>();

        _ = container.AddTransient<FixManager>();
        _ = container.AddTransient<ArchiveTools>();

        _ = container.AddSingleton<IFilesDownloader, FilesDownloader>();
        _ = container.AddSingleton<FilesUploader>();
        _ = container.AddSingleton<ProgressReport>();
        _ = container.AddSingleton<ISteamTools, SteamTools>();
        _ = container.AddSingleton<S3Provider>();

        _ = container.AddHttpClient(string.Empty)
            .ConfigureHttpClient((serviceProvider, client) =>
            {
                var config = serviceProvider.GetRequiredService<IConfigProvider>();
                client.DefaultRequestHeaders.Add("User-Agent", "Superheater");
                client.Timeout = TimeSpan.FromSeconds(30);
            })
            .RemoveAllLoggers();

        return container;
    }
}
