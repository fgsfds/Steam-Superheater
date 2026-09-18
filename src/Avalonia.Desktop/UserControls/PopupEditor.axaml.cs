using Avalonia.Controls;
using Avalonia.Desktop.ViewModels.Popups;

namespace Avalonia.Desktop.UserControls;

public sealed partial class PopupEditor : UserControl
{
    public PopupEditor()
    {
        if (Design.IsDesignMode)
        {
            DataContext = new PopupEditorViewModel();
        }

        InitializeComponent();
    }
}

