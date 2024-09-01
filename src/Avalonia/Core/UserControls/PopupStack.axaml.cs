using Avalonia.Controls;
using Avalonia.Core.ViewModels.Popups;
using Common.Client.DI;
using Microsoft.Extensions.DependencyInjection;

namespace Avalonia.Core.UserControls;

public sealed partial class PopupStack : UserControl
{
    public PopupStack()
    {
        var vm = BindingsManager.Provider.GetRequiredService<PopupStackViewModel>();

        DataContext = vm;

        InitializeComponent();
    }
}

