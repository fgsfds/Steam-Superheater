using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Superheater.Avalonia.Core.Windows
{
    public sealed partial class MessageBox : Window
    {
        public MessageBox()
        {
            InitializeComponent();

            TextBlock.Text = $"""
Superheater doesn't have write access to
{Directory.GetCurrentDirectory()}
and can't be launched. 
Move it to the folder where you have write access.
""";
        }

        private void ButtonClick(object? sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }
    }
}
