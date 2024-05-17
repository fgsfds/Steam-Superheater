using Avalonia.Controls;
using ClientCommon.DI;
using Microsoft.Extensions.DependencyInjection;
using Superheater.Avalonia.Core.ViewModels;

namespace Superheater.Avalonia.Core.Pages
{
    public sealed partial class SettingsPage : UserControl
    {
        public SettingsPage()
        {
            var vm = BindingsManager.Provider.GetRequiredService<SettingsViewModel>();

            InitializeComponent();

            DataContext = vm;
        }
    }
}
