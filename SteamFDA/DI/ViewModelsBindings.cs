using SimpleInjector;
using SteamFDA.ViewModels;

namespace SteamFDA.DI
{
    public static class ViewModelsBindings
    {
        public static void Load(Container container)
        {
            container.Register<MainWindowViewModel>(Lifestyle.Singleton);
            container.Register<MainViewModel>(Lifestyle.Transient);
            container.Register<EditorViewModel>(Lifestyle.Transient);
            container.Register<NewsViewModel>(Lifestyle.Transient);
            container.Register<SettingsViewModel>(Lifestyle.Transient);
        }
    }
}
