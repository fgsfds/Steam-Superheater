using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Common.Entities;
using System;
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
            var style = Application.Current.Resources.TryGetValue("ResourceKey", out var value);

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
