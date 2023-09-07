using Avalonia.Controls;

namespace SteamFDA.UserControls
{
    public partial class EditorFields : UserControl
    {
        public EditorFields()
        {
            InitializeComponent();
        }

        private void VersionTextBoxChanging(object sender, TextChangingEventArgs e)
        {
            if (VersionTextBox.Text == "")
            {
                VersionTextBox.Text = "0";
            }
            else
            {
                VersionTextBox.Text = ((TextBox)sender).Text;
            }
        }

        private void VersionTextBoxChanged(object sender, TextChangedEventArgs e)
        {
            if (VersionTextBox.Text == "0")
            {
                VersionTextBox.SelectAll();
            }
        }
    }
}
