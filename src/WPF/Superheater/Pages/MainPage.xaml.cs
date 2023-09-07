using SteamFD.ViewModels;
using SteamFDCommon.DI;
using System.Windows.Controls;

namespace SteamFD.Pages
{
    /// <summary>
    /// Interaction logic for MainForm.xaml
    /// </summary>
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
