using Avalonia.Controls;
using Avalonia.Desktop.ViewModels.Popups;

namespace Avalonia.Desktop.UserControls;

public sealed partial class PopupStack : UserControl
{
    public PopupStack()
    {
        if (Design.IsDesignMode)
        {
            DataContext = new PopupStackViewModel();
        }

        InitializeComponent();
    }
}

