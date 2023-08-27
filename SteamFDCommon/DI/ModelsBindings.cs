using SimpleInjector;
using SteamFDCommon.Models;
using Container = SimpleInjector.Container;

namespace SteamFDCommon.DI
{
    public static class ModelsBindings
    {
        public static void Load(Container container)
        {
            container.Register<EditorModel>(Lifestyle.Singleton);
            container.Register<MainModel>(Lifestyle.Singleton);
            container.Register<NewsModel>(Lifestyle.Singleton);
            container.Register<AboutModel>(Lifestyle.Singleton);
        }
    }
}
