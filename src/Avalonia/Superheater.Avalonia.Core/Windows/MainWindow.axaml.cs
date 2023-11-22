using Avalonia.Controls;
using Common.DI;
using Microsoft.Extensions.DependencyInjection;
using Superheater.Avalonia.Core.ViewModels;

namespace Superheater.Avalonia.Core.Windows;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        var vm = BindingsManager.Provider.GetRequiredService<MainWindowViewModel>();

        DataContext = vm;

        InitializeComponent();
    }
}
