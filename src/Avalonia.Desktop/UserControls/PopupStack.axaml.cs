using Avalonia.Controls;
using Avalonia.Desktop.ViewModels.Popups;
using Common.Client.DI;
using Microsoft.Extensions.DependencyInjection;

namespace Avalonia.Desktop.UserControls;

public sealed partial class PopupStack : UserControl
{
    public PopupStack()
    {
        if (Design.IsDesignMode)
        {
            DataContext = new PopupStackViewModel();
        }
        else
        {
            DataContext = BindingsManager.Provider.GetRequiredService<PopupStackViewModel>();
        }

        InitializeComponent();
    }
}

