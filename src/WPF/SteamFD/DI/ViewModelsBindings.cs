using SimpleInjector;
using SteamFD.ViewModels;

namespace SteamFD.DI
{
    public static class ViewModelsBindings
    {
        public static void Load(Container container)
        {
            container.Register<MainViewModel>(Lifestyle.Transient);
            container.Register<EditorViewModel>(Lifestyle.Transient);
            container.Register<NewsViewModel>(Lifestyle.Transient);
            container.Register<SettingsViewModel>(Lifestyle.Transient);
        }
    }
}
