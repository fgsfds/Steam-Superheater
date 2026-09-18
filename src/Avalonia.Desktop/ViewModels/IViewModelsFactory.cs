using Avalonia.Desktop.ViewModels.Editor;
using Avalonia.Desktop.ViewModels.Popups;

namespace Avalonia.Desktop.ViewModels;

/// <summary>
/// Creates and provides the application view models.
/// </summary>
internal interface IViewModelsFactory
{
    /// <summary>
    /// Gets the main window view model.
    /// </summary>
    /// <returns>The main window view model.</returns>
    MainWindowViewModel GetMainWindowViewModel();

    /// <summary>
    /// Gets the main page view model.
    /// </summary>
    /// <returns>The main page view model.</returns>
    MainViewModel GetMainViewModel();

    /// <summary>
    /// Gets the editor page view model.
    /// </summary>
    /// <returns>The editor page view model.</returns>
    EditorViewModel GetEditorViewModel();

    /// <summary>
    /// Gets the news page view model.
    /// </summary>
    /// <returns>The news page view model.</returns>
    NewsViewModel GetNewsViewModel();

    /// <summary>
    /// Gets the settings page view model.
    /// </summary>
    /// <returns>The settings page view model.</returns>
    SettingsViewModel GetSettingsViewModel();

    /// <summary>
    /// Gets the about page view model.
    /// </summary>
    /// <returns>The about page view model.</returns>
    AboutViewModel GetAboutViewModel();

    /// <summary>
    /// Gets the sources page view model.
    /// </summary>
    /// <returns>The sources page view model.</returns>
    SourcesViewModel GetSourcesViewModel();

    /// <summary>
    /// Gets the message popup view model.
    /// </summary>
    /// <returns>The message popup view model.</returns>
    PopupMessageViewModel GetPopupMessageViewModel();

    /// <summary>
    /// Gets the editor popup view model.
    /// </summary>
    /// <returns>The editor popup view model.</returns>
    PopupEditorViewModel GetPopupEditorViewModel();

    /// <summary>
    /// Gets the stack popup view model.
    /// </summary>
    /// <returns>The stack popup view model.</returns>
    PopupStackViewModel GetPopupStackViewModel();
}
