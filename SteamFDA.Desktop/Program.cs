using System;
using System.IO;
using Avalonia;
using SteamFDCommon;
using SteamFDCommon.Updater;

namespace SteamFDA.Desktop;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    //public static void Main(string[] args) => BuildAvaloniaApp()
    //    .StartWithClassicDesktopLifetime(args);

    public static void Main(string[] args)
    {
        if (File.Exists(Consts.UpdateFile))
        {
            //var updateInstaller = new UpdateInstaller();

            //updateInstaller.InstallUpdate();
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
