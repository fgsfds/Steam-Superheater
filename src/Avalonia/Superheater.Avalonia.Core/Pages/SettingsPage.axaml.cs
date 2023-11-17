using Avalonia.Controls;
using Common.DI;
using Superheater.Avalonia.Core.ViewModels;

namespace Superheater.Avalonia.Core.Pages
{
    public sealed partial class SettingsPage : UserControl
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
