using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace SteamFDA.Helpers
{
    internal static class Properties
    {
        public static Window MainWindow => ((IClassicDesktopStyleApplicationLifetime)Application.Current.ApplicationLifetime).MainWindow;

        public static TopLevel TopLevel => TopLevel.GetTopLevel(MainWindow);
    }
}
