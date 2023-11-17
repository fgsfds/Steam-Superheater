using Avalonia.Controls;
using Common.DI;
using Superheater.Avalonia.Core.ViewModels;

namespace Superheater.Avalonia.Core.Windows;

public sealed partial class MainWindow : Window
{
    private readonly MainWindowViewModel _mwvm;

    public MainWindow()
    {
        _mwvm = BindingsManager.Instance.GetInstance<MainWindowViewModel>();

        DataContext = _mwvm;

        InitializeComponent();
    }
}
