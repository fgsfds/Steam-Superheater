using SimpleInjector;
using SteamFDCommon.Config;
using SteamFDTCommon.Providers;

namespace SteamFDCommon.DI
{
    public static class CommonBindings
    {
        public static void Load(Container container)
        {
            container.Register<ConfigProvider>(Lifestyle.Singleton);
            container.Register<GamesProvider>(Lifestyle.Singleton);
            container.Register<NewsProvider>(Lifestyle.Singleton);
            container.Register<FixesProvider>(Lifestyle.Singleton);
            container.Register<InstalledFixesProvider>(Lifestyle.Singleton);
            container.Register<UpdateInstaller>(Lifestyle.Singleton);
        }
    }
}
