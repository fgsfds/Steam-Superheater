using Microsoft.Extensions.DependencyInjection;
using Superheater.Avalonia.Core.ViewModels;

namespace Superheater.Avalonia.Core.DI
{
    public static class ViewModelsBindings
    {
        public static void Load(ServiceCollection container)
        {
            container.AddSingleton<MainWindowViewModel>();
            container.AddSingleton<MainViewModel>();
            container.AddSingleton<EditorViewModel>();
            container.AddSingleton<NewsViewModel>();
            container.AddSingleton<SettingsViewModel>();
            container.AddSingleton<AboutViewModel>();

            container.AddSingleton<PopupEditorViewModel>();
            container.AddSingleton<PopupMessageViewModel>();
        }
    }
}
