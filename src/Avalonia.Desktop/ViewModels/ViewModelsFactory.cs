using Avalonia.Desktop.ViewModels.Editor;
using Avalonia.Desktop.ViewModels.Popups;

namespace Avalonia.Desktop.ViewModels;

/// <summary>
/// Default <see cref="IViewModelsFactory" /> implementation backed by the DI container.
/// </summary>
internal sealed class ViewModelsFactory : IViewModelsFactory
{
    private readonly AboutViewModel _aboutViewModel;
    private readonly EditorViewModel _editorViewModel;
    private readonly MainViewModel _mainViewModel;
    private readonly MainWindowViewModel _mainWindowViewModel;
    private readonly NewsViewModel _newsViewModel;
    private readonly PopupEditorViewModel _popupEditorViewModel;
    private readonly PopupMessageViewModel _popupMessageViewModel;
    private readonly PopupStackViewModel _popupStackViewModel;
    private readonly SettingsViewModel _settingsViewModel;
    private readonly SourcesViewModel _sourcesViewModel;

    /// <summary>
    /// Initializes a new instance of the <see cref="ViewModelsFactory" /> class.
    /// </summary>
    /// <param name="mainWindowViewModel">The main window view model.</param>
    /// <param name="mainViewModel">The main page view model.</param>
    /// <param name="editorViewModel">The editor page view model.</param>
    /// <param name="newsViewModel">The news page view model.</param>
    /// <param name="settingsViewModel">The settings page view model.</param>
    /// <param name="aboutViewModel">The about page view model.</param>
    /// <param name="sourcesViewModel">The sources page view model.</param>
    /// <param name="popupMessageViewModel">The message popup view model.</param>
    /// <param name="popupEditorViewModel">The editor popup view model.</param>
    /// <param name="popupStackViewModel">The stack popup view model.</param>
    public ViewModelsFactory(
        MainWindowViewModel mainWindowViewModel,
        MainViewModel mainViewModel,
        EditorViewModel editorViewModel,
        NewsViewModel newsViewModel,
        SettingsViewModel settingsViewModel,
        AboutViewModel aboutViewModel,
        SourcesViewModel sourcesViewModel,
        PopupMessageViewModel popupMessageViewModel,
        PopupEditorViewModel popupEditorViewModel,
        PopupStackViewModel popupStackViewModel
        )
    {
        _mainWindowViewModel = mainWindowViewModel;
        _mainViewModel = mainViewModel;
        _editorViewModel = editorViewModel;
        _newsViewModel = newsViewModel;
        _settingsViewModel = settingsViewModel;
        _aboutViewModel = aboutViewModel;
        _sourcesViewModel = sourcesViewModel;
        _popupMessageViewModel = popupMessageViewModel;
        _popupEditorViewModel = popupEditorViewModel;
        _popupStackViewModel = popupStackViewModel;
    }

    /// <inheritdoc />
    public MainWindowViewModel GetMainWindowViewModel() => _mainWindowViewModel;

    /// <inheritdoc />
    public MainViewModel GetMainViewModel() => _mainViewModel;

    /// <inheritdoc />
    public EditorViewModel GetEditorViewModel() => _editorViewModel;

    /// <inheritdoc />
    public NewsViewModel GetNewsViewModel() => _newsViewModel;

    /// <inheritdoc />
    public SettingsViewModel GetSettingsViewModel() => _settingsViewModel;

    /// <inheritdoc />
    public AboutViewModel GetAboutViewModel() => _aboutViewModel;

    /// <inheritdoc />
    public SourcesViewModel GetSourcesViewModel() => _sourcesViewModel;

    /// <inheritdoc />
    public PopupMessageViewModel GetPopupMessageViewModel() => _popupMessageViewModel;

    /// <inheritdoc />
    public PopupEditorViewModel GetPopupEditorViewModel() => _popupEditorViewModel;

    /// <inheritdoc />
    public PopupStackViewModel GetPopupStackViewModel() => _popupStackViewModel;
}
