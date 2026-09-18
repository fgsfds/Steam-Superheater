using Avalonia.Desktop.ViewModels;
using Avalonia.Desktop.ViewModels.Editor;
using Avalonia.Desktop.ViewModels.Popups;
using Microsoft.Extensions.DependencyInjection;

namespace Avalonia.Desktop.DI;

/// <summary>
/// Registers the desktop view models.
/// </summary>
public static class ViewModelsBindings
{
    /// <summary>
    /// Registers the desktop view models and the view models factory.
    /// </summary>
    /// <param name="container">The service collection.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection WithViewModels(this IServiceCollection container)
    {
        _ = container.AddSingleton<MainWindowViewModel>();
        _ = container.AddSingleton<MainViewModel>();
        _ = container.AddSingleton<EditorViewModel>();
        _ = container.AddSingleton<NewsViewModel>();
        _ = container.AddSingleton<SettingsViewModel>();
        _ = container.AddSingleton<AboutViewModel>();
        _ = container.AddSingleton<SourcesViewModel>();

        _ = container.AddSingleton<PopupEditorViewModel>();
        _ = container.AddSingleton<PopupMessageViewModel>();
        _ = container.AddSingleton<PopupStackViewModel>();

        _ = container.AddSingleton<IViewModelsFactory, ViewModelsFactory>();

        return container;
    }
}
