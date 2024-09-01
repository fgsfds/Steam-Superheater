using Avalonia.Core.ViewModels;
using Avalonia.Core.ViewModels.Popups;
using Microsoft.Extensions.DependencyInjection;

namespace Avalonia.Core.DI;

public static class ViewModelsBindings
{
    public static void Load(ServiceCollection container)
    {
        _ = container.AddSingleton<MainWindowViewModel>();
        _ = container.AddSingleton<MainViewModel>();
        _ = container.AddSingleton<EditorViewModel>();
        _ = container.AddSingleton<NewsViewModel>();
        _ = container.AddSingleton<SettingsViewModel>();
        _ = container.AddSingleton<AboutViewModel>();

        _ = container.AddSingleton<PopupEditorViewModel>();
        _ = container.AddSingleton<PopupMessageViewModel>();
        _ = container.AddSingleton<PopupStackViewModel>();
    }
}

