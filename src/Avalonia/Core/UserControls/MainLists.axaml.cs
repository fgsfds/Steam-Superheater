using Avalonia.Controls;

namespace Avalonia.Core.UserControls;

public sealed partial class MainLists : UserControl
{
    public MainLists()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Stupid hack that resets scroll in the games list to the selected game
    /// </summary>
    private void ListBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (GamesListBox.SelectedIndex != -1)
        {
            GamesListBox.ScrollIntoView(GamesListBox.SelectedIndex);
            GamesListBox.ScrollIntoView(GamesListBox.SelectedIndex);
        }
    }
}

