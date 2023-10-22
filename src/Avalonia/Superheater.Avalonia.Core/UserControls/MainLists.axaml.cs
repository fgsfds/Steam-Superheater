using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Common.Entities;
using System.Diagnostics;

namespace Superheater.Avalonia.Core.UserControls
{
    public sealed partial class MainLists : UserControl
    {
        public MainLists()
        {
            InitializeComponent();
        }

        //formatting description text
        private void FixSelected(object sender, SelectionChangedEventArgs e)
        {
            DescriptionBox.Children.Clear();

            if (((ListBox)sender).SelectedItem is not FixEntity fixEntity)
            {
                return;
            }

            var description = fixEntity.Description;

            if (description is null)
            {
                return;
            }

            var splitDescription = description.Split('\n');

            foreach (var item in splitDescription)
            {
                if (item.StartsWith("*") && item.EndsWith("*"))
                {
                    var text = item[1..^1];
                    DescriptionBox.Children.Add(new TextBlock() { Text = text, FontWeight = FontWeight.Bold, TextWrapping = TextWrapping.Wrap });

                    continue;
                }
                else if (item.StartsWith("http"))
                {
                    var button = new Button
                    {
                        Content = item
                    };

                    button.Click += UrlButtonClick;

                    DescriptionBox.Children.Add(button);

                    continue;
                }

                DescriptionBox.Children.Add(new TextBlock() { Text = item, TextWrapping = TextWrapping.Wrap });
            }
        }

        private void UrlButtonClick(object? sender, RoutedEventArgs e)
        {
            if (sender is not Button button) throw new Exception("Sender is not a button");

            var url = button.Content?.ToString() ?? throw new NullReferenceException(nameof(button.Content));

            Process.Start(new ProcessStartInfo(url)
            {
                UseShellExecute = true
            });
        }
    }
}
