using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Superheater.Avalonia.Core.Windows
{
    public sealed partial class MessageBox : Window
    {
        public MessageBox()
        {
        }

        public MessageBox(string text)
        {
            InitializeComponent();

            TextBlock.Text = text;
        }

        private void ButtonClick(object? sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }
    }
}
