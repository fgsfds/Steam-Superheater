using Avalonia.Controls;

namespace Superheater.Avalonia.Core.UserControls
{
    public sealed partial class EditorFields : UserControl
    {
        public EditorFields()
        {
            InitializeComponent();
        }

        private void VersionTextBoxChanging(object sender, TextChangingEventArgs e)
        {
            VersionTextBox.Text = string.IsNullOrWhiteSpace(VersionTextBox.Text)
                ? "0"
                : VersionTextBox.Text;
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
