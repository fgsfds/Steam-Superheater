using SimpleInjector;
using Superheater.Avalonia.Core.ViewModels;

namespace Superheater.Avalonia.Core.DI
{
    public static class ViewModelsBindings
    {
        public static void Load(Container container)
        {
            container.Register<MainWindowViewModel>(Lifestyle.Singleton);
            container.Register<MainViewModel>(Lifestyle.Singleton);
            container.Register<EditorViewModel>(Lifestyle.Singleton);
            container.Register<NewsViewModel>(Lifestyle.Singleton);
            container.Register<SettingsViewModel>(Lifestyle.Singleton);
            container.Register<AboutViewModel>(Lifestyle.Singleton);
        }
    }
}
