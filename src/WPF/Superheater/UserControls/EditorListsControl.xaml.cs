using System.Windows.Controls;

namespace Superheater.UserControls
{
    /// <summary>
    /// Interaction logic for ListsControl.xaml
    /// </summary>
    public sealed partial class EditorListsControl : UserControl
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
