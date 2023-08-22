using System.Windows.Controls;

namespace SteamFD.UserControls
{
    /// <summary>
    /// Interaction logic for ListsControl.xaml
    /// </summary>
    public partial class EditorListsControl : UserControl
    {
        public EditorListsControl()
        {
            InitializeComponent();
        }

        private void GamesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                ((ListBox)sender).ScrollIntoView(e.AddedItems[0]);
            }
        }
    }
}
