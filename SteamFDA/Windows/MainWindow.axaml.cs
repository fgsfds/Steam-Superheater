using Avalonia.Controls;
using SteamFDA.ViewModels;
using SteamFDCommon.DI;

namespace SteamFDA.Windows;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _mwvm;

    public MainWindow()
    {
        _mwvm = BindingsManager.Instance.GetInstance<MainWindowViewModel>();

        DataContext = _mwvm;

        InitializeComponent();
    }
}
