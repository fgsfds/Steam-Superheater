using Common.FixTools;
using Common.FixTools.FileFix;
using Common.FixTools.RegistryFix;
using SimpleInjector;

namespace Common.DI
{
    public static class CommonBindings
    {
        public static void Load(Container container)
        {
            container.Register<AppUpdateInstaller>(Lifestyle.Singleton);

            container.Register<FileFixInstaller>(Lifestyle.Singleton);
            container.Register<FileFixUpdater>(Lifestyle.Singleton);
            container.Register<FileFixUninstaller>(Lifestyle.Singleton);

            container.Register<RegistryFixInstaller>(Lifestyle.Singleton);
            container.Register<RegistryFixUpdater>(Lifestyle.Singleton);
            container.Register<RegistryFixUninstaller>(Lifestyle.Singleton);

            container.Register<FixManager>(Lifestyle.Singleton);
        }
    }
}
