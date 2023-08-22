using SteamFD.ViewModels;
using SteamFDCommon.DI;
using System.Windows.Controls;

namespace SteamFD.Pages
{
    /// <summary>
    /// Interaction logic for NewsControl.xaml
    /// </summary>
    public partial class NewsPage : UserControl
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
