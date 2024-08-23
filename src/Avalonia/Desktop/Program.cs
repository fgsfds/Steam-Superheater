using Avalonia;
using Common.Client;
using Common.Client.DI;
using Common.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Projektanker.Icons.Avalonia;
using Projektanker.Icons.Avalonia.FontAwesome;
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

        if (args.Contains("-deck"))
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
                BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            }
            catch (Exception ex)
            {
                var logger = BindingsManager.Provider.GetRequiredService<Logger>();
                logger.Error(ex.ToString());

                Environment.FailFast(ex.ToString());
            }
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    private static AppBuilder BuildAvaloniaApp()
    {
        IconProvider.Current
            .Register<FontAwesomeIconProvider>()
            //.Register<MaterialDesignIconProvider>()
            ;

        return AppBuilder.Configure<App>()
                .UsePlatformDetect()
                //.WithInterFont()
                .LogToTrace();
    }
}
