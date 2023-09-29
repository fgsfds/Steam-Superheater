using Avalonia.Controls;
using Superheater.Avalonia.Core.ViewModels;
using Common.DI;

namespace Superheater.Avalonia.Core.Pages
{
    public sealed partial class EditorPage : UserControl
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
