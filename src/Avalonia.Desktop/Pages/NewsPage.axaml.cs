using Avalonia.Controls;
using Avalonia.Desktop.ViewModels;
using Common.Axiom.Helpers;
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

    private void ScrollViewer_ScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        Guard2.IsOfType<ScrollViewer>(sender, out var scrollViewer);
        Guard2.IsOfType<NewsViewModel>(DataContext, out var newsViewModel);

        var offset = scrollViewer.Offset.Y;
        var ext = scrollViewer.Extent.Height - scrollViewer.Bounds.Height;

        if (ext - offset < 100)
        {
            newsViewModel.LoadNextPageCommand.Execute(null);
        }
    }
}

