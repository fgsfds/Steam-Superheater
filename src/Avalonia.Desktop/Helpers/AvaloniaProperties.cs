using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Diagnostics;

namespace Avalonia.Desktop.Helpers;

public static class AvaloniaProperties
{
    public static Window MainWindow
    {
        get
        {
            var mainWindow = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;

            Guard.IsNotNull(mainWindow);

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