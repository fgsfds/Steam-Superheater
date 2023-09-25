using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Interactivity;
using System.Diagnostics;

namespace Superheater.Avalonia.Core.UserControls
{
    public partial class EditorButtons : UserControl
    {
        public EditorButtons()
        {
            InitializeComponent();

            //fix for inconsistent combobox width
            GamesComboBox.ItemsPanel = new FuncTemplate<Panel>(new(() => new StackPanel()));
        }

        private void HowToSubmitButtonClick(object sender, RoutedEventArgs e)
        {
            Process.Start("explorer.exe", "https://github.com/fgsfds/Steam-Superheater/wiki/How-to-submit-fixes");
        }
    }
}
