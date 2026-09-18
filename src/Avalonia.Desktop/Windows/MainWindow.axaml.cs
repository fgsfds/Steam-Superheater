using Avalonia;
using Avalonia.Controls;
using Avalonia.Desktop.ViewModels;
using Avalonia.Desktop.ViewModels.Popups;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Common.Axiom;

namespace Avalonia.Desktop.Windows;

/// <summary>
/// The main application window.
/// </summary>
public sealed partial class MainWindow : Window
{
    private readonly IConfigProvider _config;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow" /> class.
    /// </summary>
    /// <remarks>
    /// Parameterless constructor used by the designer.
    /// </remarks>
    public MainWindow()
    {
        _config = null!;

        RenderOptions.SetBitmapInterpolationMode(this, BitmapInterpolationMode.HighQuality);

        InitializeComponent();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow" /> class.
    /// </summary>
    /// <param name="viewModelsFactory">The view models factory.</param>
    /// <param name="config">The configuration provider.</param>
    internal MainWindow(IViewModelsFactory viewModelsFactory, IConfigProvider config)
    {
        _config = config;

        RenderOptions.SetBitmapInterpolationMode(this, BitmapInterpolationMode.HighQuality);

        InitializeComponent();

        MainPageTab.DataContext = viewModelsFactory.GetMainViewModel();
        SourcesPageTab.DataContext = viewModelsFactory.GetSourcesViewModel();
        EditorPageTab.DataContext = viewModelsFactory.GetEditorViewModel();
        NewsPageTab.DataContext = viewModelsFactory.GetNewsViewModel();
        SettingsPageTab.DataContext = viewModelsFactory.GetSettingsViewModel();
        AboutPageTab.DataContext = viewModelsFactory.GetAboutViewModel();

        PopupMessage.DataContext = viewModelsFactory.GetPopupMessageViewModel();
        PopupEditor.DataContext = viewModelsFactory.GetPopupEditorViewModel();
        PopupStack.DataContext = viewModelsFactory.GetPopupStackViewModel();

        ((IPopup)PopupEditor.DataContext!).PopupShownEvent += Popup_IsShown;
        ((IPopup)PopupMessage.DataContext!).PopupShownEvent += Popup_IsShown;
        ((IPopup)PopupStack.DataContext!).PopupShownEvent += Popup_IsShown;

        viewModelsFactory.GetMainViewModel().InitializeCommand.Execute(null);
        viewModelsFactory.GetNewsViewModel().InitializeCommand.Execute(null);
        viewModelsFactory.GetAboutViewModel().InitializeCommand.Execute(null);

        EnableSystemBackdrop();
    }

    /// <summary>
    /// Switches the selected tab.
    /// </summary>
    /// <param name="tabEnum">The tab to select.</param>
    public void SwitchTab(MainWindowTabsEnum tabEnum)
    {
        if (tabEnum is MainWindowTabsEnum.MainTab)
        {
            MainTab.IsSelected = true;
        }
    }

    private void Popup_IsShown(bool obj)
    {
        if (obj)
        {
            Tabs.Effect = new BlurEffect() { Radius = 5 };
            Tabs.IsHitTestVisible = false;
        }
        else
        {
            Tabs.Effect = null;
            Tabs.IsHitTestVisible = true;
        }
    }


    /// <summary>
    /// Requests a system backdrop on Windows, falling back to acrylic and blur when Mica is unavailable.
    /// </summary>
    private void EnableSystemBackdrop()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        PropertyChanged += OnWindowPropertyChanged;
        _config.ParameterChangedEvent += OnConfigParameterChanged;

        ApplyBackdrop();
    }

    /// <summary>
    /// Applies or removes the system backdrop according to the current configuration.
    /// </summary>
    private void ApplyBackdrop()
    {
        TransparencyLevelHint = _config.UseMica
            ?
            [
                WindowTransparencyLevel.Mica,
                WindowTransparencyLevel.AcrylicBlur,
                WindowTransparencyLevel.Blur,
                WindowTransparencyLevel.None
            ]
            : [WindowTransparencyLevel.None];

        UpdateBackdropState();
    }

    /// <summary>
    /// Re-applies the system backdrop when the Mica setting changes.
    /// </summary>
    /// <param name="parameterName">The name of the changed configuration parameter.</param>
    private void OnConfigParameterChanged(string parameterName)
    {
        if (parameterName == nameof(IConfigProvider.UseMica))
        {
            ApplyBackdrop();
        }
    }

    /// <summary>
    /// Enables the transparent window background when the platform provided an actual system backdrop.
    /// </summary>
    private void UpdateBackdropState()
    {
        var hasBackdrop =
            ActualTransparencyLevel.Equals(WindowTransparencyLevel.Mica)
         || ActualTransparencyLevel.Equals(WindowTransparencyLevel.AcrylicBlur)
         || ActualTransparencyLevel.Equals(WindowTransparencyLevel.Blur);

        Classes.Set("mica", hasBackdrop);
    }

    /// <summary>
    /// Updates the backdrop state when the platform changes the achieved transparency level.
    /// </summary>
    /// <param name="sender">The event source.</param>
    /// <param name="e">The property change arguments.</param>
    private void OnWindowPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property != ActualTransparencyLevelProperty)
        {
            return;
        }

        UpdateBackdropState();
    }
}

/// <summary>
/// Main window tabs.
/// </summary>
public enum MainWindowTabsEnum
{
    /// <summary>
    /// The main tab.
    /// </summary>
    MainTab,

    /// <summary>
    /// The editor tab.
    /// </summary>
    EditorTab,

    /// <summary>
    /// The news tab.
    /// </summary>
    NewsTab,

    /// <summary>
    /// The settings tab.
    /// </summary>
    SettingsTab,

    /// <summary>
    /// The about tab.
    /// </summary>
    AboutTab
}
