using Avalonia.Controls;

namespace Avalonia.Desktop.UserControls.Editor;

public partial class EditorRegFixControl : UserControl
{
    public EditorRegFixControl()
    {
        InitializeComponent();
    }

    private void Carousel_Next(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Slides.Previous();
    }

    private void Carousel_Prev(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Slides.Next();
    }
}