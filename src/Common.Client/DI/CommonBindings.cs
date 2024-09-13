using Api.Common.Interface;
using Common.Client.FilesTools;
using Common.Client.FixTools;
using Common.Client.FixTools.FileFix;
using Common.Client.FixTools.HostsFix;
using Common.Client.FixTools.RegistryFix;
using Common.Client.Logger;
using Microsoft.Extensions.DependencyInjection;

namespace Common.Client.DI;

public static class CommonBindings
{
    public static void Load(ServiceCollection container, bool isDesigner)
    {
        if (isDesigner)
        {
            _ = container.AddSingleton<ILogger, LoggerToConsole>();
        }
        else
        {
            _ = container.AddSingleton<ILogger, LoggerToFile>();
        }

        _ = container.AddTransient<AppUpdateInstaller>();

        _ = container.AddTransient<FileFixInstaller>();
        _ = container.AddTransient<FileFixUpdater>();
        _ = container.AddTransient<FileFixUninstaller>();

        _ = container.AddTransient<RegistryFixInstaller>();
        _ = container.AddTransient<RegistryFixUpdater>();
        _ = container.AddTransient<RegistryFixUninstaller>();

        _ = container.AddTransient<HostsFixInstaller>();
        _ = container.AddTransient<HostsFixUpdater>();
        _ = container.AddTransient<HostsFixUninstaller>();

        _ = container.AddTransient<FixManager>();
        _ = container.AddTransient<ArchiveTools>();

        _ = container.AddSingleton<FilesDownloader>();
        _ = container.AddSingleton<FilesUploader>();
        _ = container.AddSingleton<ProgressReport>();
        _ = container.AddSingleton<HttpClient>(CreateHttpClient);
        _ = container.AddSingleton<SteamTools>();
        _ = container.AddSingleton<ApiInterface>();
    }

    private static HttpClient CreateHttpClient(IServiceProvider provider)
    {
        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("User-Agent", "Superheater");
        httpClient.Timeout = TimeSpan.FromSeconds(10);
        return httpClient;
    }
}

