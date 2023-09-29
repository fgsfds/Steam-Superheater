using Avalonia.Controls;
using Superheater.Avalonia.Core.ViewModels;
using Common.DI;

namespace Superheater.Avalonia.Core.Pages
{
    public sealed partial class NewsPage : UserControl
    {
        private readonly NewsViewModel _nvm;

        public NewsPage()
        {
            _nvm = BindingsManager.Instance.GetInstance<NewsViewModel>();

            DataContext = _nvm;

            InitializeComponent();

            _nvm.InitializeCommand.Execute(null);
        }
    }
}
