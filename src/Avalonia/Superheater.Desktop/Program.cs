using Avalonia;
using Common;
using Common.Helpers;
using Superheater.Avalonia.Core;
using Superheater.Avalonia.Core.Helpers;

namespace Superheater.Desktop;

internal static class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        if (args.Contains("-dev"))
        {
            Logger.Info("Started in developer mode");

            Properties.IsDeveloperMode = true;
        }

        var dir = Directory.GetCurrentDirectory();

        if (File.Exists(Path.Combine(dir, Consts.UpdateFile)))
        {
            Logger.Info("Update file detected");

            AppUpdateInstaller.InstallUpdate();
        }
        else
        {
            Cleanup();

            try
            {
                BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString());

                if (!Properties.IsDeveloperMode)
                {
                    Logger.UploadLog();
                }

                Environment.FailFast(ex.ToString());
            }
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    private static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            //.WithInterFont()
            .LogToTrace();

    /// <summary>
    /// Remove update leftovers
    /// </summary>
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
