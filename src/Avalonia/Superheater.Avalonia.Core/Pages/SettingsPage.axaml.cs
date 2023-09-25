using Avalonia.Controls;
using Superheater.Avalonia.Core.ViewModels;
using Common.DI;

namespace Superheater.Avalonia.Core.Pages
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
