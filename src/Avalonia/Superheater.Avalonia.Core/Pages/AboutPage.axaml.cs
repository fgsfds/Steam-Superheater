using Avalonia.Controls;
using Avalonia.Interactivity;
using SteamFDA.ViewModels;
using SteamFDCommon.DI;
using System.Diagnostics;

namespace SteamFDA.Pages
{
    public partial class AboutPage : UserControl
    {
        private readonly AboutViewModel _avm;

        public AboutPage()
        {
            _avm = BindingsManager.Instance.GetInstance<AboutViewModel>();

            DataContext = _avm;

            InitializeComponent();
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
    }
}
