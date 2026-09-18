using Avalonia.Controls;
using Avalonia.Desktop.ViewModels;
using Avalonia.Desktop.ViewModels.Popups;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace Avalonia.Desktop.Windows;

/// <summary>
/// The main application window.
/// </summary>
public sealed partial class MainWindow : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow" /> class.
    /// </summary>
    /// <remarks>
    /// Parameterless constructor used by the designer and headless tests.
    /// </remarks>
    public MainWindow()
    {
        RenderOptions.SetBitmapInterpolationMode(this, BitmapInterpolationMode.HighQuality);

        InitializeComponent();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow" /> class.
    /// </summary>
    /// <param name="viewModelsFactory">The view models factory.</param>
    internal MainWindow(IViewModelsFactory viewModelsFactory)
    {
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
