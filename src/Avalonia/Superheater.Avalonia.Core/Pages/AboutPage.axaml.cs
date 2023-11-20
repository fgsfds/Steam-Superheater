using Avalonia.Controls;
using Avalonia.Interactivity;
using Common.DI;
using Superheater.Avalonia.Core.ViewModels;
using System.Diagnostics;

namespace Superheater.Avalonia.Core.Pages
{
    public sealed partial class AboutPage : UserControl
    {
        private readonly AboutViewModel _avm;

        public AboutPage()
        {
            _avm = BindingsManager.Instance.GetInstance<AboutViewModel>();

            DataContext = _avm;

            InitializeComponent();

            _avm.InitializeCommand.Execute(null);
        }

        private void SteamClick(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://steamcommunity.com/id/hasnogames/",
                UseShellExecute = true
            });
        }

        private void DiscordClick(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/fgsfds/Steam-Superheater/wiki/Useful-links",
                UseShellExecute = true
            });
        }

        private void GitHubClick(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/fgsfds/Steam-Superheater",
                UseShellExecute = true
            });
        }

        private void GitHubIssuesClick(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/fgsfds/Steam-Superheater/issues/new",
                UseShellExecute = true
            });
        }

        private void ShowChangelogClick(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/fgsfds/Steam-Superheater/releases",
                UseShellExecute = true
            });
        }
    }
}
