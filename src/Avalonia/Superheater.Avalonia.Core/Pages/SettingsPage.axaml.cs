using Avalonia.Controls;
using SteamFDA.ViewModels;
using SteamFDCommon.DI;

namespace SteamFDA.Pages
{
    public partial class SettingsPage : UserControl
    {
        private readonly SettingsViewModel _svm;

        public SettingsPage()
        {
            InitializeComponent();

            _svm = BindingsManager.Instance.GetInstance<SettingsViewModel>();

            DataContext = _svm;
        }
    }
}
