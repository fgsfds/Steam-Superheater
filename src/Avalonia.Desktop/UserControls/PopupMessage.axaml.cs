using Avalonia.Controls;
using Avalonia.Desktop.ViewModels.Popups;
using Common.Client.DI;
using Microsoft.Extensions.DependencyInjection;

namespace Avalonia.Desktop.UserControls;

public sealed partial class PopupMessage : UserControl
{
    public PopupMessage()
    {
        if (Design.IsDesignMode)
        {
            DataContext = new PopupMessageViewModel();
        }
        else
        {

            DataContext = BindingsManager.Provider.GetRequiredService<PopupMessageViewModel>();
        }

        InitializeComponent();
    }
}

