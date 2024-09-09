using Avalonia.Controls;
using Avalonia.Desktop.ViewModels.Popups;
using Common.Client.DI;
using Microsoft.Extensions.DependencyInjection;

namespace Avalonia.Desktop.UserControls;

public sealed partial class PopupEditor : UserControl
{
    public PopupEditor()
    {
        var vm = BindingsManager.Provider.GetRequiredService<PopupEditorViewModel>();

        DataContext = vm;

        InitializeComponent();
    }
}

