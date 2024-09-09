using Avalonia.Controls;
using Avalonia.Desktop.ViewModels.Popups;
using Common.Client.DI;
using Microsoft.Extensions.DependencyInjection;

namespace Avalonia.Desktop.UserControls;

public sealed partial class PopupStack : UserControl
{
    public PopupStack()
    {
        var vm = BindingsManager.Provider.GetRequiredService<PopupStackViewModel>();

        DataContext = vm;

        InitializeComponent();
    }
}

