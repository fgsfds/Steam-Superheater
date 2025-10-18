using Avalonia;
using Avalonia.Desktop;
using Avalonia.Headless;
using Common.Client;
using Common.Client.DI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Projektanker.Icons.Avalonia;
using Projektanker.Icons.Avalonia.FontAwesome;
using Tests.UI;

[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]

public class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp()
    {
        _ = IconProvider.Current.Register<FontAwesomeIconProvider>();

        App.LoadBindings();
        _ = BindingsManager.Instance.RemoveAll<ISteamTools>();
        _ = BindingsManager.Instance.AddSingleton<ISteamTools, SteamToolsStub>();

        return AppBuilder
            .Configure<App>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions() { UseHeadlessDrawing = false })
            .UseSkia()
            .WithInterFont()
        ;
    }
}
