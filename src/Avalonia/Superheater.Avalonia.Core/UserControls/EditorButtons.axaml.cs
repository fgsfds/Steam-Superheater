using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Interactivity;
using System.Diagnostics;

namespace Superheater.Avalonia.Core.UserControls
{
    public sealed partial class EditorButtons : UserControl
    {
        public EditorButtons()
        {
            InitializeComponent();

            //fix for inconsistent combobox width
            GamesComboBox.ItemsPanel = new FuncTemplate<Panel?>(() => new StackPanel());
        }

        private void HowToSubmitButtonClick(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/fgsfds/Steam-Superheater/wiki/How-to-submit-fixes",
                UseShellExecute = true
            });
        }
    }
}
