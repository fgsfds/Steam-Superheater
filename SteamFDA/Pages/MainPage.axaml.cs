using Avalonia.Controls;
using SteamFDA.ViewModels;
using SteamFDCommon.DI;

namespace SteamFDA.Pages
{
    public partial class MainPage : UserControl
    {
        private readonly MainViewModel _mvm;

        public MainPage()
        {
            _mvm = BindingsManager.Instance.GetInstance<MainViewModel>();

            DataContext = _mvm;

            InitializeComponent();

            _mvm.InitializeCommand.Execute(null);
        }
    }
}
