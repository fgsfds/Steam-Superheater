using Avalonia.Controls;
using Superheater.Avalonia.Core.ViewModels;
using Common.DI;

namespace Superheater.Avalonia.Core.Pages
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
