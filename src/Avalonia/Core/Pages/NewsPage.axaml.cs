using Avalonia.Controls;
using Common.DI;
using Microsoft.Extensions.DependencyInjection;
using Superheater.Avalonia.Core.ViewModels;

namespace Superheater.Avalonia.Core.Pages
{
    public sealed partial class NewsPage : UserControl
    {
        public NewsPage()
        {
            var vm = BindingsManager.Provider.GetRequiredService<NewsViewModel>();

            DataContext = vm;

            InitializeComponent();

            vm.InitializeCommand.Execute(null);
        }
    }
}
