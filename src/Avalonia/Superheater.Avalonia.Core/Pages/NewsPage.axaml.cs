using Avalonia.Controls;
using SteamFDA.ViewModels;
using SteamFDCommon.DI;

namespace SteamFDA.Pages
{
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
