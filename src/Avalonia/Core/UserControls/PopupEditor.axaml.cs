using Avalonia.Controls;
using ClientCommon.DI;
using Microsoft.Extensions.DependencyInjection;
using Superheater.Avalonia.Core.ViewModels.Popups;

namespace Superheater.Avalonia.Core.UserControls
{
    public sealed partial class PopupEditor : UserControl
    {
        public PopupEditor()
        {
            var vm = BindingsManager.Provider.GetRequiredService<PopupEditorViewModel>();

            DataContext = vm;

            InitializeComponent();
        }
    }
}
