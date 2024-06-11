using Avalonia.Controls;
using Common.Client.DI;
using Microsoft.Extensions.DependencyInjection;
using Superheater.Avalonia.Core.ViewModels;

namespace Superheater.Avalonia.Core.Pages
{
    public sealed partial class EditorPage : UserControl
    {
        public EditorPage()
        {
            var vm = BindingsManager.Provider.GetRequiredService<EditorViewModel>();

            DataContext = vm;

            InitializeComponent();

            vm.InitializeCommand.Execute(null);
        }
    }
}
