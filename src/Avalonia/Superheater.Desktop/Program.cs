using System;
using System.IO;
using Avalonia;
using SteamFDCommon;
using SteamFDCommon.Helpers;

namespace SteamFDA.Desktop;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        if (File.Exists(Consts.UpdateFile))
        {
            UpdateInstaller.InstallUpdate();

            Environment.Exit(0);
        }
        else
        {
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            //.WithInterFont()
            .LogToTrace();

}
