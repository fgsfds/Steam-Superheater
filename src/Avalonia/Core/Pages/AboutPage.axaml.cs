using Avalonia.Controls;
using Avalonia.Interactivity;
using Common.DI;
using Microsoft.Extensions.DependencyInjection;
using Superheater.Avalonia.Core.ViewModels;
using System.Diagnostics;

namespace Superheater.Avalonia.Core.Pages
{
    public sealed partial class AboutPage : UserControl
    {
        public AboutPage()
        {
            var vm = BindingsManager.Provider.GetRequiredService<AboutViewModel>();

            DataContext = vm;

            InitializeComponent();

            vm.InitializeCommand.Execute(null);
        }

        private void DiscordClick(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://discord.gg/mWvKyxR4et",
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
