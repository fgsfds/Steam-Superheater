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
        if (sender is not ScrollViewer scrollViewer)
        {
            throw new ArgumentException("Sender is not a scroll viewer.", nameof(sender));
        }

        if (DataContext is not NewsViewModel newsViewModel)
        {
            throw new ArgumentException("Data context is not a news view model.", nameof(DataContext));
        }

        var offset = scrollViewer.Offset.Y;
        var ext = scrollViewer.Extent.Height - scrollViewer.Bounds.Height;

        if (ext - offset < 100)
        {
            newsViewModel.LoadNextPageCommand.Execute(null);
        }
    }
}

