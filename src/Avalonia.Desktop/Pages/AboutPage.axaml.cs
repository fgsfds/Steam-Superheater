using Avalonia.Controls;
using Avalonia.Desktop.ViewModels;
using Avalonia.Interactivity;
using Common.Client.DI;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;

namespace Avalonia.Desktop.Pages;

public sealed partial class AboutPage : UserControl
{
    public AboutPage()
    {
        var vm = BindingsManager.Provider.GetRequiredService<AboutViewModel>();

        DataContext = vm;

        InitializeComponent();

        vm.InitializeCommand.Execute(null);
    }

    private void PatreonClick(object sender, RoutedEventArgs e)
    {
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = "https://www.patreon.com/fgsfds",
            UseShellExecute = true
        });
    }

    private void DiscordClick(object sender, RoutedEventArgs e)
    {
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = "https://discord.gg/mWvKyxR4et",
            UseShellExecute = true
        });
    }

    private void GitHubClick(object sender, RoutedEventArgs e)
    {
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = "https://github.com/fgsfds/Steam-Superheater",
            UseShellExecute = true
        });
    }

    private void GitHubIssuesClick(object sender, RoutedEventArgs e)
    {
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = "https://github.com/fgsfds/Steam-Superheater/issues/new",
            UseShellExecute = true
        });
    }

    private void ShowChangelogClick(object sender, RoutedEventArgs e)
    {
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = "https://github.com/fgsfds/Steam-Superheater/releases",
            UseShellExecute = true
        });
    }

    private void SignPathClick(object sender, RoutedEventArgs e)
    {
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = "https://about.signpath.io/",
            UseShellExecute = true
        });
    }
}

