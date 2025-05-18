using Avalonia.Controls;
using Avalonia.Desktop.ViewModels;
using Common.Client.DI;
using Microsoft.Extensions.DependencyInjection;

namespace Avalonia.Desktop.Pages;

public sealed partial class SourcesPage : UserControl
{
    public SourcesPage()
    {
        var vm = BindingsManager.Provider.GetRequiredService<SourcesViewModel>();
        DataContext = vm;

        InitializeComponent();
    }
}

