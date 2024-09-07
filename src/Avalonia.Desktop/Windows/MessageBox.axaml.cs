using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Avalonia.Desktop.Windows;

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
        Environment.FailFast(string.Empty);
    }
}

