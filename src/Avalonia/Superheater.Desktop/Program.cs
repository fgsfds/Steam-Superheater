using Avalonia;
using Common;
using Common.Helpers;
using Superheater.Avalonia.Core;
using Superheater.Avalonia.Core.Helpers;

namespace Superheater.Desktop;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        if (args.Contains("-dev"))
        {
            Properties.IsDeveloperMode = true;
        }

        var dir = Directory.GetCurrentDirectory();

        if (File.Exists(Path.Combine(dir, Consts.UpdateFile)))
        {
            UpdateInstaller.InstallUpdate();
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
                Logger.Log(Environment.OSVersion.ToString());
                Logger.Log(CommonProperties.CurrentVersion.ToString());
                Logger.Log(ex.ToString());

                Logger.UploadLog();
            }
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
