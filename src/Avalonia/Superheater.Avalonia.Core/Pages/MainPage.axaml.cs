using Avalonia.Controls;
using Common.DI;
using Superheater.Avalonia.Core.ViewModels;

namespace Superheater.Avalonia.Core.Pages
{
    public sealed partial class MainPage : UserControl
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
