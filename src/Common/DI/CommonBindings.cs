using SimpleInjector;
using Common.FixTools;

namespace Common.DI
{
    public static class CommonBindings
    {
        public static void Load(Container container)
        {
            container.Register<UpdateInstaller>(Lifestyle.Singleton);
            container.Register<FixInstaller>(Lifestyle.Singleton);
            container.Register<FixUpdater>(Lifestyle.Singleton);
            //container.Register<FixUninstaller>(Lifestyle.Singleton);
        }
    }
}
