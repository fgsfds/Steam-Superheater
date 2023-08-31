using Avalonia.Controls;
using SteamFDA.ViewModels;
using SteamFDCommon.DI;

namespace SteamFDA.Pages
{
    public partial class EditorPage : UserControl
    {
        private readonly EditorViewModel _evm;

        public EditorPage()
        {
            _evm = BindingsManager.Instance.GetInstance<EditorViewModel>();

            DataContext = _evm;

            InitializeComponent();

            _evm.InitializeCommand.Execute(null);
        }
    }
}
