using Avalonia.Controls;
using Avalonia.Core.ViewModels;
using Common.Client.DI;
using Microsoft.Extensions.DependencyInjection;

namespace Avalonia.Core.Pages;

public sealed partial class MainPage : UserControl
{
    public MainPage()
    {
        var vm = BindingsManager.Provider.GetRequiredService<MainViewModel>();

        DataContext = vm;

        InitializeComponent();

        vm.InitializeCommand.Execute(null);
    }
}

