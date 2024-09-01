using Avalonia.Core;
using Common.Client;
using Common.Helpers;
using Projektanker.Icons.Avalonia;
using Projektanker.Icons.Avalonia.FontAwesome;
using System.Runtime.ExceptionServices;

namespace Avalonia.Desktop;

public static class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        if (args.Contains("--crash"))
        {
            ClientProperties.HasCrashed = new(true, args[1]);
        }

        if (args.Contains("--dev"))
        {
            ClientProperties.IsDeveloperMode = true;
        }

        if (args.Contains("--deck"))
        {
            ClientProperties.IsInSteamDeckGameMode = true;
        }

        if (File.Exists(Path.Combine(ClientProperties.WorkingFolder, Consts.UpdateFile)))
        {
            AppUpdateInstaller.InstallUpdate();
        }
        else
        {
            try
            {
                AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
                _ = BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            }
            catch (Exception ex)
            {
                ExceptionDispatchInfo.Capture(ex).Throw();
            }
        }
    }

    private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var exe = Path.Combine(ClientProperties.WorkingFolder, ClientProperties.ExecutableName);
        var args = "--crash " + $@"""{e.ExceptionObject}""";

        _ = System.Diagnostics.Process.Start(exe, args);

        Environment.Exit(-1);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    private static AppBuilder BuildAvaloniaApp()
    {
        _ = IconProvider.Current.Register<FontAwesomeIconProvider>();

        return AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();
    }
}