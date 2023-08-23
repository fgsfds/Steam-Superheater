using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Diagnostics;

namespace SteamFDA.Pages
{
    public partial class AboutPage : UserControl
    {
        public AboutPage()
        {
            InitializeComponent();
        }

        private void SteamClick(object sender, RoutedEventArgs e)
        {
            Process.Start("explorer.exe", "https://steamcommunity.com/id/hasnogames/");
        }

        private void GitHubClick(object sender, RoutedEventArgs e)
        {
            Process.Start("explorer.exe", "https://github.com/fgsfds/Steam-Fixes-Downloader");
        }
    }
}
