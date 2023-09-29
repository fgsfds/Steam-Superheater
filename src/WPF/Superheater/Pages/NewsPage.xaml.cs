using Superheater.ViewModels;
using Common.DI;
using System.Windows.Controls;

namespace Superheater.Pages
{
    /// <summary>
    /// Interaction logic for NewsControl.xaml
    /// </summary>
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
