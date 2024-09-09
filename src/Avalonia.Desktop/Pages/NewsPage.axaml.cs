using Avalonia.Controls;
using Avalonia.Desktop.ViewModels;
using Common.Client.DI;
using Microsoft.Extensions.DependencyInjection;

namespace Avalonia.Desktop.Pages;

public sealed partial class NewsPage : UserControl
{
    public NewsPage()
    {
        var vm = BindingsManager.Provider.GetRequiredService<NewsViewModel>();

        DataContext = vm;

        InitializeComponent();

        vm.InitializeCommand.Execute(null);
    }
}

