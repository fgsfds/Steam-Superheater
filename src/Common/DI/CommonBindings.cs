using Common.FixTools;
using Common.FixTools.FileFix;
using Common.FixTools.HostsFix;
using Common.FixTools.RegistryFix;
using Common.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace Common.DI
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

            container.AddTransient<FileTools>();

            container.AddSingleton<ProgressReport>();
        }
    }
}
