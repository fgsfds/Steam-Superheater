using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Common.Config;
using Common.DI;
using Common.Enums;
using Common.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Superheater.Avalonia.Core.DI;
using Superheater.Avalonia.Core.Pages;
using Superheater.Avalonia.Core.Windows;

namespace Superheater.Avalonia.Core;

public sealed partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var container = BindingsManager.Instance;

        ModelsBindings.Load(container);
        ViewModelsBindings.Load(container);
        CommonBindings.Load(container);
        ProvidersBindings.Load(container);

        var theme = BindingsManager.Provider.GetRequiredService<ConfigProvider>()?.Config.Theme;

        var themeEnum = theme switch
        {
            ThemeEnum.System => ThemeVariant.Default,
            ThemeEnum.Light => ThemeVariant.Light,
            ThemeEnum.Dark => ThemeVariant.Dark,
            _ => ThrowHelper.ArgumentOutOfRangeException<ThemeVariant>(theme.ToString())
        };

        RequestedThemeVariant = themeEnum;

        // Line below is needed to remove Avalonia data validation.
        // Without this line you will get duplicate validations from both Avalonia and CT
        BindingPlugins.DataValidators.RemoveAt(0);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new MainPage();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
