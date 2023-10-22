using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace Superheater.Avalonia.Core.Helpers
{
    public static class Properties
    {
        public static Window MainWindow => (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow
            ?? throw new NullReferenceException(nameof(MainWindow));

        public static TopLevel TopLevel => TopLevel.GetTopLevel(MainWindow)
            ?? throw new NullReferenceException(nameof(TopLevel));

        /// <summary>
        /// Is app started in developer mode
        /// </summary>
        public static bool IsDeveloperMode { get; set; }
    }
}
