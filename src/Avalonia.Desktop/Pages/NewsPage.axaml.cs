using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Desktop.ViewModels;
using Avalonia.Interactivity;
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

    private void ScrollViewer_ScrollChanged(object? sender, Avalonia.Controls.ScrollChangedEventArgs e)
    {
        var offset = ((ScrollViewer)sender).Offset.Y;
        var ext = ((ScrollViewer)sender).Extent.Height - ((ScrollViewer)sender).Bounds.Height;

        if (ext - offset < 100)
        {
            ((NewsViewModel)DataContext).LoadNextPageCommand.Execute(null);
        }

        //var percent = (offset / ext) * 100;

        //if (percent > 90)
        //{
        //    ((NewsViewModel)DataContext).LoadNextPageCommand.Execute(null);
        //}
    }
}

 