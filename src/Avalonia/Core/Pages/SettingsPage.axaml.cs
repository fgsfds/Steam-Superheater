using Avalonia.Controls;
using Avalonia.Core.ViewModels;
using Common.Client.DI;
using Microsoft.Extensions.DependencyInjection;

namespace Avalonia.Core.Pages;

public sealed partial class SettingsPage : UserControl
{
    public SettingsPage()
    {
        var vm = BindingsManager.Provider.GetRequiredService<SettingsViewModel>();

        InitializeComponent();

        DataContext = vm;
    }
}

