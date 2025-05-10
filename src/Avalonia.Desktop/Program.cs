using Common.Client;
using Common.Helpers;
using Projektanker.Icons.Avalonia;
using Projektanker.Icons.Avalonia.FontAwesome;
using System.Runtime.InteropServices;

namespace Avalonia.Desktop;

public sealed partial class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static int Main(string[] args)
    {
        if (File.Exists(Path.Combine(ClientProperties.WorkingFolder, Consts.UpdateFile)))
        {
            AppUpdateInstaller.InstallUpdate();
            return 0;
        }

        if (args.Contains("--dev"))
        {
            ClientProperties.IsDeveloperMode = true;
        }

        if (args.Contains("--deck"))
        {
            ClientProperties.IsInSteamDeckGameMode = true;
        }

        if (args.Contains("--offline"))
        {
            ClientProperties.IsOfflineMode = true;
        }

        try
        {
            var builder = BuildAvaloniaApp();

            return App.Run(builder);
        }
        catch (Exception ex)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                WinMsgBox.Show(
                    "Critical error",
                    ex.ToString()
                    );
            }

            if (File.Exists(ClientProperties.PathToLogFile))
            {
                File.Copy(ClientProperties.PathToLogFile, Path.Combine(ClientProperties.WorkingFolder, $"{DateTime.Now:dd_MM_yy_HH_mm}.crashlog"));
            }

            return -1;
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    private static AppBuilder BuildAvaloniaApp()
    {
        _ = IconProvider.Current
            .Register<FontAwesomeIconProvider>()
            ;

        return AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();
    }

    private static partial class WinMsgBox
    {
        [LibraryImport("user32.dll", EntryPoint = "MessageBoxW", StringMarshalling = StringMarshalling.Utf16)]
        private static partial int MessageBox(IntPtr hWnd, string? text, string? caption, int type);

        public static void Show(string? title, string? text)
        {
            _ = MessageBox(IntPtr.Zero, text, title, 0);
        }
    }
}
