using Avalonia;
using ClientCommon;
using Common.Helpers;
using Superheater.Avalonia.Core;

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
            ClientProperties.IsDeveloperMode = true;
        }

        if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), Consts.UpdateFile)))
        {
            AppUpdateInstaller.InstallUpdate();
        }
        else
        {
            try
            {
                BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            }
            catch (Exception ex)
            {
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
}
