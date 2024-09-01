using Avalonia.Controls;
using Avalonia.Core.ViewModels;
using Common.Client.DI;
using Microsoft.Extensions.DependencyInjection;

namespace Avalonia.Core.Pages;

public sealed partial class EditorPage : UserControl
{
    public EditorPage()
    {
        var vm = BindingsManager.Provider.GetRequiredService<EditorViewModel>();

        DataContext = vm;

        InitializeComponent();
    }
}

