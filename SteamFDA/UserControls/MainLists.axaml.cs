using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using SteamFDTCommon.Entities;
using System.Diagnostics;

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
            stack.Children.Clear();

            if (((ListBox)sender).SelectedItem is null)
            {
                return;
            }
            var description = ((FixEntity)((ListBox)sender).SelectedItem).Description;

            var splitDescription = description.Split('\n');

            foreach(var item in splitDescription )
            {
                if (item.StartsWith("*"))
                {
                    var text = item[1..];
                    stack.Children.Add((
                        new TextBlock() { Text = text, FontWeight = FontWeight.Black, TextWrapping = TextWrapping.Wrap })
                        );
                    continue;
                }
                else if (item.StartsWith("http"))
                {
                    var button = new Button
                    {
                        Content = item,
                        Background = new SolidColorBrush(Colors.White) { Opacity = 0 }
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
