using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace Superheater.Avalonia.Core.Helpers
{
    public static class Properties
    {
        public static Window MainWindow => ((IClassicDesktopStyleApplicationLifetime)Application.Current.ApplicationLifetime).MainWindow;

        public static TopLevel TopLevel => TopLevel.GetTopLevel(MainWindow);

        public static bool IsDeveloperMode { get; set; }
    }
}
