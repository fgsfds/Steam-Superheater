using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using SteamFDA.DI;
using SteamFDA.Pages;
using SteamFDA.Windows;
using SteamFDCommon.Config;
using SteamFDCommon.DI;
using System;

namespace SteamFDA;

public partial class App : Application
{
    public override void Initialize()
    {
        var container = BindingsManager.Instance;
        container.Options.EnableAutoVerification = false;
        container.Options.ResolveUnregisteredConcreteTypes = true;

        ModelsBindings.Load(container);
        ViewModelsBindings.Load(container);
        CommonBindings.Load(container);

        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var theme = BindingsManager.Instance.GetInstance<ConfigProvider>().Config.Theme;

        var themeEnum = theme switch
        {
            "System" => ThemeVariant.Default,
            "Light" => ThemeVariant.Light,
            "Dark" => ThemeVariant.Dark,
            _ => throw new ArgumentOutOfRangeException(theme)
        }; ;

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
