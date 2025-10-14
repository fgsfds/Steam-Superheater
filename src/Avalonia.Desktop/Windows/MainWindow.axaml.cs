using Avalonia.Controls;
using Avalonia.Desktop.ViewModels.Popups;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace Avalonia.Desktop.Windows;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        RenderOptions.SetBitmapInterpolationMode(this, BitmapInterpolationMode.HighQuality);

#if DEBUG
        this.AttachDevTools();
#endif

        InitializeComponent();

        ((IPopup)PopupEditor.DataContext!).PopupShownEvent += Popup_IsShown;
        ((IPopup)PopupMessage.DataContext!).PopupShownEvent += Popup_IsShown;
        ((IPopup)PopupStack.DataContext!).PopupShownEvent += Popup_IsShown;
    }

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

public enum MainWindowTabsEnum
{
    MainTab,
    EditorTab,
    NewsTab,
    SettingsTab,
    AboutTab
}