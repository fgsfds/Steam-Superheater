using Superheater.ViewModels;
using Common.DI;
using System.Windows.Controls;

namespace Superheater.Pages
{
    /// <summary>
    /// Interaction logic for EditorControl.xaml
    /// </summary>
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
