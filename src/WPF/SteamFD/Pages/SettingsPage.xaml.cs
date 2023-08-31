using SteamFD.ViewModels;
using SteamFDCommon.DI;
using System.Windows.Controls;
using System.Windows.Input;

namespace SteamFD.Pages
{
    /// <summary>
    /// Interaction logic for SettingsPage.xaml
    /// </summary>
    public partial class SettingsPage : UserControl
    {
        private readonly SettingsViewModel _svm;

        public SettingsPage()
        {
            InitializeComponent();

            _svm = BindingsManager.Instance.GetInstance<SettingsViewModel>();

            DataContext = _svm;
        }

        private void TextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                if (_svm.SaveLocalRepoPathCommand.CanExecute(null))
                {
                    _svm.SaveLocalRepoPathCommand.Execute(null);
                }
            }
        }
    }
}
