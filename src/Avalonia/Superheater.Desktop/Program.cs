using System;
using System.IO;
using System.Linq;
using Avalonia;
using Common;
using Common.Helpers;
using Superheater.Avalonia.Core;

namespace Superheater.Desktop;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        if (args.Contains("-dev"))
        {
            CommonProperties.IsDeveloperMode = true;
        }

        var dir = Directory.GetCurrentDirectory();

        if (File.Exists(Path.Combine(dir, Consts.UpdateFile)))
        {
            UpdateInstaller.InstallUpdate();
        }
        else
        {
            Cleanup();

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

    private static void Cleanup()
    {
        var files = Directory.GetFiles(Directory.GetCurrentDirectory());

        foreach (var file in files)
        {
            if (file.EndsWith(".old") || file.EndsWith(".temp") || file.Equals(Consts.UpdateFile))
            {
                File.Delete(file);
            }

            var updateDir = Path.Combine(Directory.GetCurrentDirectory(), Consts.UpdateFolder);

            if (Directory.Exists(updateDir))
            {
                Directory.Delete(updateDir, true);
            }
        }
    }

}
