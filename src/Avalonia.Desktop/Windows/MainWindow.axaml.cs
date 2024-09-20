using Avalonia.Controls;
using Avalonia.Desktop.ViewModels;
using Avalonia.Desktop.ViewModels.Popups;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Common.Client.DI;
using Microsoft.Extensions.DependencyInjection;

namespace Avalonia.Desktop.Windows;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        RenderOptions.SetBitmapInterpolationMode(this, BitmapInterpolationMode.HighQuality);

        var vm = BindingsManager.Provider.GetRequiredService<MainWindowViewModel>();

        DataContext = vm;

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
        }
        else
        {
            Tabs.Effect = null;
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