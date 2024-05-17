using Avalonia.Controls;
using ClientCommon.DI;
using Microsoft.Extensions.DependencyInjection;
using Superheater.Avalonia.Core.ViewModels.Popups;

namespace Superheater.Avalonia.Core.UserControls
{
    public sealed partial class PopupStack : UserControl
    {
        public PopupStack()
        {
            var vm = BindingsManager.Provider.GetRequiredService<PopupStackViewModel>();

            DataContext = vm;

            InitializeComponent();
        }
    }
}
