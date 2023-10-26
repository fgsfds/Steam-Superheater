using SimpleInjector;
using Common.Config;
using Common.Providers;

namespace Common.DI
{
    public static class ProvidersBindings
    {
        public static void Load(Container container)
        {
            container.Register<CombinedEntitiesProvider>(Lifestyle.Singleton);
            container.Register<ConfigProvider>(Lifestyle.Singleton);
            container.Register<GamesProvider>(Lifestyle.Singleton);
            container.Register<NewsProvider>(Lifestyle.Singleton);
            container.Register<FixesProvider>(Lifestyle.Singleton);
        }
    }
}
