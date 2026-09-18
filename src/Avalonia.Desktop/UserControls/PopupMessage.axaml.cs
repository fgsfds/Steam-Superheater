using Avalonia.Controls;
using Avalonia.Desktop.ViewModels.Popups;

namespace Avalonia.Desktop.UserControls;

public sealed partial class PopupMessage : UserControl
{
    public PopupMessage()
    {
        if (Design.IsDesignMode)
        {
            DataContext = new PopupMessageViewModel();
        }

        InitializeComponent();
    }
}

