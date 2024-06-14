using Common.Client.API;
using Common.Client.Config;
using Common.Client.FixTools;
using Common.Client.FixTools.FileFix;
using Common.Client.FixTools.HostsFix;
using Common.Client.FixTools.RegistryFix;
using Microsoft.Extensions.DependencyInjection;

namespace Common.Client.DI
{
    public static class CommonBindings
    {
        public static void Load(ServiceCollection container)
        {
            container.AddTransient<AppUpdateInstaller>();

            container.AddTransient<FileFixInstaller>();
            container.AddTransient<FileFixUpdater>();
            container.AddTransient<FileFixUninstaller>();

            container.AddTransient<RegistryFixInstaller>();
            container.AddTransient<RegistryFixUpdater>();
            container.AddTransient<RegistryFixUninstaller>();

            container.AddTransient<HostsFixInstaller>();
            container.AddTransient<HostsFixUpdater>();
            container.AddTransient<HostsFixUninstaller>();

            container.AddTransient<FixManager>();
            container.AddTransient<ArchiveTools>();

            container.AddSingleton<ProgressReport>();
            container.AddSingleton<HttpClient>(CreateHttpClient);
            container.AddSingleton<FilesUploader>();
            container.AddSingleton<SteamTools>();
            container.AddSingleton<Logger>();
            container.AddSingleton<ApiInterface>();

            container.AddSingleton<DatabaseContextFactory>();
        }

        private static HttpClient CreateHttpClient(IServiceProvider provider)
        {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Superheater");
            return httpClient;
        }
    }
}
