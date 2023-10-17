using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using System;

namespace Superheater.Avalonia.Core.Helpers
{
    public static class Properties
    {
        public static Window MainWindow => (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow
            ?? throw new NullReferenceException(nameof(MainWindow));

        public static TopLevel TopLevel => TopLevel.GetTopLevel(MainWindow)
            ?? throw new NullReferenceException(nameof(TopLevel));

        public static bool IsDeveloperMode { get; set; }
    }
}
