using Avalonia.Controls;
using Avalonia.Core.ViewModels;
using Common.Client.DI;
using Microsoft.Extensions.DependencyInjection;

namespace Avalonia.Core.Pages;

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

