using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Desktop.Windows;
using Common.Helpers;
using CommunityToolkit.Diagnostics;

namespace Avalonia.Desktop.Helpers;

public static class AvaloniaProperties
{
    public static MainWindow MainWindow
    {
        get
        {
            var window = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;

            Guard2.IsOfType<MainWindow>(window, out var mainWindow);

            return mainWindow;
        }
    }

    public static TopLevel TopLevel
    {
        get
        {
            var topLevel = TopLevel.GetTopLevel(MainWindow);

            Guard.IsNotNull(topLevel);

            return topLevel;
        }
    }
}