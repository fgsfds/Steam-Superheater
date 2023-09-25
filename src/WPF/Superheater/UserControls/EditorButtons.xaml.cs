using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Superheater.UserControls
{
    /// <summary>
    /// Interaction logic for EditorButtons.xaml
    /// </summary>
    public partial class EditorButtons : UserControl
    {
        public EditorButtons()
        {
            InitializeComponent();
        }

        private void HowToSubmitButtonClick(object sender, RoutedEventArgs e)
        {
            Process.Start("explorer.exe", "https://github.com/fgsfds/Steam-Superheater/wiki/How-to-submit-fixes");
        }
    }
}
