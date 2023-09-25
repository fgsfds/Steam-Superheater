using SteamFDCommon.Entities;
using System.Diagnostics;
using System.Windows;
using System;
using System.Windows.Controls;
using System.Reflection.Emit;
using System.Windows.Media;

namespace SteamFD.UserControls
{
    /// <summary>
    /// Interaction logic for ListsControl.xaml
    /// </summary>
    public partial class MainListsControl : UserControl
    {
        public MainListsControl()
        {
            InitializeComponent();

            //FixesList.ItemContainerStyle = this.FindResource("ListBoxStyleOrange") as Style;
        }

        private void GamesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                ((ListBox)sender).ScrollIntoView(e.AddedItems[0]);
            }
        }
        private void FixSelected(object sender, SelectionChangedEventArgs e)
        {
            DescriptionBox.Children.Clear();

            if (((ListBox)sender).SelectedItem is null)
            {
                return;
            }

            var description = ((FixEntity)((ListBox)sender).SelectedItem).Description;

            var splitDescription = description.Split('\n');

            foreach (var item in splitDescription)
            {
                if (item.StartsWith("*") && item.EndsWith("*"))
                {
                    var text = item[1..^1];
                    DescriptionBox.Children.Add(new TextBlock() { Text = text, FontWeight = FontWeights.Bold, TextWrapping = TextWrapping.Wrap });

                    continue;
                }
                else if (item.StartsWith("http"))
                {
                    var button = new Button
                    {
                        Content = item,
                        Background = null,
                        BorderBrush = null,
                        HorizontalContentAlignment = HorizontalAlignment.Left,
                        Foreground = new SolidColorBrush(Colors.Blue),
                    };

                button.Click += UrlButtonClick;

                DescriptionBox.Children.Add(button);

                continue;
                }

                DescriptionBox.Children.Add(new TextBlock() { Text = item, TextWrapping = TextWrapping.Wrap });
            }
        }

        private void UrlButtonClick(object sender, RoutedEventArgs e)
        {
            var url = ((Button)sender)?.Content?.ToString() ?? throw new NullReferenceException();

            Process.Start(new ProcessStartInfo(url)
            {
                UseShellExecute = true
            });
        }
    }
}
