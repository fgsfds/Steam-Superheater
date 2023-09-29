using Avalonia.Controls;
using Avalonia.Interactivity;
using Superheater.Avalonia.Core.ViewModels;
using Common.DI;
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
            Process.Start("explorer.exe", "https://steamcommunity.com/id/hasnogames/");
        }

        private void GitHubClick(object sender, RoutedEventArgs e)
        {
            Process.Start("explorer.exe", "https://github.com/fgsfds/Steam-Superheater");
        }

        private void GitHubIssuesClick(object sender, RoutedEventArgs e)
        {
            Process.Start("explorer.exe", "https://github.com/fgsfds/Steam-Superheater/issues/new");
        }

        private void ShowChangelogClick(object sender, RoutedEventArgs e)
        {
            Process.Start("explorer.exe", "https://github.com/fgsfds/Steam-Superheater/releases");
        }
    }
}
