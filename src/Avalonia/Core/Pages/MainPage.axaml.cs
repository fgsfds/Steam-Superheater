using Avalonia.Controls;
using Common.Client.DI;
using Microsoft.Extensions.DependencyInjection;
using Superheater.Avalonia.Core.ViewModels;

namespace Superheater.Avalonia.Core.Pages
{
    public sealed partial class MainPage : UserControl
    {
        public MainPage()
        {
            var vm = BindingsManager.Provider.GetRequiredService<MainViewModel>();

            DataContext = vm;

            InitializeComponent();

            vm.InitializeCommand.Execute(null);
        }
    }
}
