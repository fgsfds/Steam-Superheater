using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
using SteamFDCommon.Entities;
using System.Diagnostics;
using System.Reflection.Emit;

namespace SteamFDA.UserControls
{
    public partial class MainLists : UserControl
    {
        public MainLists()
        {
            InitializeComponent();
        }


        private void FixSelected(object sender, SelectionChangedEventArgs e)
        {
            var style = Application.Current.Resources.TryGetValue("ResourceKey", out var value);

            stack.Children.Clear();

            if (((ListBox)sender).SelectedItem is null)
            {
                return;
            }
            var description = ((FixEntity)((ListBox)sender).SelectedItem).Description;

            var splitDescription = description.Split('\n');

            foreach(var item in splitDescription )
            {
                if (item.StartsWith("*") && item.EndsWith("*"))
                {
                    var text = item[1..^1];
                    stack.Children.Add((
                        new TextBlock() { Text = text, FontWeight = FontWeight.Bold, TextWrapping = TextWrapping.Wrap })
                        );
                    continue;
                }
                else if (item.StartsWith("http"))
                {
                    var button = new Button
                    {
                        Content = item
                    };

                    button.Click += ButtonClick;

                    stack.Children.Add(button);
                    continue;
                }



                stack.Children.Add((new TextBlock() { Text = item, TextWrapping = TextWrapping.Wrap }));
            }
        }

        private void ButtonClick(object sender, RoutedEventArgs e)
        {
            var url = ((Button)sender).Content.ToString();

            Process.Start("explorer.exe", url);
        }
    }
}
