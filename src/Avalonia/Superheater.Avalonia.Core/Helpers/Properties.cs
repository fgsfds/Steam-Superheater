using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace Superheater.Avalonia.Core.Helpers
{
    internal static class Properties
    {
        public static Window MainWindow => ((IClassicDesktopStyleApplicationLifetime)Application.Current.ApplicationLifetime).MainWindow;

        public static TopLevel TopLevel => TopLevel.GetTopLevel(MainWindow);
    }
}
