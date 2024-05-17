using Avalonia.Controls;
using Avalonia.Media;
using ClientCommon.DI;
using Microsoft.Extensions.DependencyInjection;
using Superheater.Avalonia.Core.ViewModels;
using Superheater.Avalonia.Core.ViewModels.Popups;

namespace Superheater.Avalonia.Core.Windows;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        var vm = BindingsManager.Provider.GetRequiredService<MainWindowViewModel>();

        DataContext = vm;

        InitializeComponent();

        ((IPopup)PopupEditor.DataContext!).PopupShownEvent += MainWindow_IsShown;
        ((IPopup)PopupMessage.DataContext!).PopupShownEvent += MainWindow_IsShown;
    }

    private void MainWindow_IsShown(bool obj)
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
