using Avalonia.Controls;
using Avalonia.Desktop.ViewModels;
using Common.Axiom.Helpers;

namespace Avalonia.Desktop.Pages;

public sealed partial class NewsPage : UserControl
{
    public NewsPage()
    {
        InitializeComponent();
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

