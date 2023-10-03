using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace Superheater.UserControls
{
    /// <summary>
    /// Interaction logic for EditorButtons.xaml
    /// </summary>
    public sealed partial class EditorButtons : UserControl
    {
        public EditorButtons()
        {
            InitializeComponent();
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
