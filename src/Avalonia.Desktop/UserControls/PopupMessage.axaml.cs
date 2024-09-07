using Avalonia.Controls;
using Avalonia.Desktop.ViewModels.Popups;
using Common.Client.DI;
using Microsoft.Extensions.DependencyInjection;

namespace Avalonia.Desktop.UserControls;

public sealed partial class PopupMessage : UserControl
{
    public PopupMessage()
    {
        var vm = BindingsManager.Provider.GetRequiredService<PopupMessageViewModel>();

        DataContext = vm;

        InitializeComponent();
    }
}

