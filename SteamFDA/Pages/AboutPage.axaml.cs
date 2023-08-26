using Avalonia.Controls;
using Avalonia.Interactivity;
using SteamFDA.ViewModels;
using SteamFDCommon.DI;
using System.Diagnostics;

namespace SteamFDA.Pages
{
    public partial class AboutPage : UserControl
    {
        private readonly AboutViewModel _svm;

        public AboutPage()
        {
            _svm = BindingsManager.Instance.GetInstance<AboutViewModel>();

            DataContext = _svm;

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
