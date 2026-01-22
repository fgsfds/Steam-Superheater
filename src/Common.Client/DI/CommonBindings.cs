using Api.Axiom.Interface;
using Common.Axiom;
using Common.Axiom.Helpers;
using Common.Client.FilesTools;
using Common.Client.FilesTools.Interfaces;
using Common.Client.FixTools;
using Common.Client.FixTools.FileFix;
using Common.Client.FixTools.HostsFix;
using Common.Client.FixTools.RegistryFix;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Common.Client.DI;

public static class CommonBindings
{
    public static void Load(ServiceCollection container, bool isDesigner)
    {
        if (!isDesigner)
        {
            _ = container.AddSingleton<ILogger>(CreateLogger);
        }

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
        //_ = container.AddSingleton<IApiInterface, ServerApiInterface>();
        _ = container.AddSingleton<IApiInterface, FileApiInterface>();

        _ = container.AddHttpClient(string.Empty)
            .ConfigureHttpClient((serviceProvider, client) =>
            {
                var config = serviceProvider.GetRequiredService<IConfigProvider>();
                client.DefaultRequestHeaders.Add("User-Agent", "Superheater");
                client.Timeout = TimeSpan.FromSeconds(30);
            })
            .RemoveAllLoggers();
    }

    private static ILogger CreateLogger(IServiceProvider service) => FileLoggerFactory.Create(ClientProperties.PathToLogFile);
}
