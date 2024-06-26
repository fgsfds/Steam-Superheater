﻿using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Common.Helpers;

namespace Superheater.Avalonia.Core.Helpers
{
    public static class AvaloniaProperties
    {
        public static Window MainWindow => (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow
            ?? ThrowHelper.ArgumentNullException<Window>(nameof(MainWindow));

        public static TopLevel TopLevel => TopLevel.GetTopLevel(MainWindow)
            ?? ThrowHelper.ArgumentNullException<TopLevel>(nameof(TopLevel));
    }
}
