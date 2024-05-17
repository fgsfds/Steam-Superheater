using ClientCommon.FixTools;
using ClientCommon.FixTools.FileFix;
using ClientCommon.FixTools.HostsFix;
using ClientCommon.FixTools.RegistryFix;
using Microsoft.Extensions.DependencyInjection;

namespace ClientCommon.DI
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
        }

        private static HttpClient CreateHttpClient(IServiceProvider provider)
        {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Superheater");
            return httpClient;
        }
    }
}
