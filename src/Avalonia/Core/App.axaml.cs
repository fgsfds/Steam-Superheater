using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Common.Client;
using Common.Client.Config;
using Common.Client.DI;
using Common.Enums;
using Common.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Superheater.Avalonia.Core.DI;
using Superheater.Avalonia.Core.Pages;
using Superheater.Avalonia.Core.Windows;

namespace Superheater.Avalonia.Core;

public sealed class App : Application
{
    private static readonly Mutex _mutex = new(false, "Superheater");
    private static Logger? _logger = null;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (!DoesHaveWriteAccess(Directory.GetCurrentDirectory()))
        {
            var messageBox = new MessageBox($"""
Superheater doesn't have write access to
{Directory.GetCurrentDirectory()}
and can't be launched. 
Move it to the folder where you have write access.
"""
);
            messageBox.Show();
            return;
        }
        if (!_mutex.WaitOne(1000, false))
        {
            var messageBox = new MessageBox($"""
You can't launch multiple instances of Superheater
"""
);
            messageBox.Show();
            return;
        }

        try
        {
            var container = BindingsManager.Instance;

            ModelsBindings.Load(container);
            ViewModelsBindings.Load(container);
            CommonBindings.Load(container);
            ProvidersBindings.Load(container, Design.IsDesignMode);

            var theme = BindingsManager.Provider.GetRequiredService<IConfigProvider>().Theme;
            _logger = BindingsManager.Provider.GetRequiredService<Logger>();

            var themeEnum = theme switch
            {
                ThemeEnum.System => ThemeVariant.Default,
                ThemeEnum.Light => ThemeVariant.Light,
                ThemeEnum.Dark => ThemeVariant.Dark,
                _ => ThrowHelper.ArgumentOutOfRangeException<ThemeVariant>(theme.ToString())
            };

            RequestedThemeVariant = themeEnum;

            if (ClientProperties.IsDeveloperMode)
            {
                _logger.Info("Started in developer mode");
            }

            Cleanup();

            // Line below is needed to remove Avalonia data validation.
            // Without this line you will get duplicate validations from both Avalonia and CT
            BindingPlugins.DataValidators.RemoveAt(0);

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow();

                desktop.Exit += OnAppExit;
            }
            else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
            {
                singleViewPlatform.MainView = new MainPage();
            }

            base.OnFrameworkInitializationCompleted();
        }
        catch (Exception ex)
        {
            var messageBox = new MessageBox(ex.Message);
            messageBox.Show();

            _logger?.Error(ex.ToString());
        }
    }

    private void OnAppExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        var httpClient = BindingsManager.Provider.GetRequiredService<HttpClient>();
        httpClient?.Dispose();
    }

    /// <summary>
    /// Check if you can write to the current directory
    /// </summary>
    private static bool DoesHaveWriteAccess(string folderPath)
    {
        try
        {
            var fs = File.Create(Path.Combine(folderPath, Path.GetRandomFileName()));
            fs.Close();
            File.Delete(fs.Name);

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Remove update leftovers
    /// </summary>
    private static void Cleanup()
    {
        _logger?.Info("Starting cleanup");

        var files = Directory.GetFiles(Directory.GetCurrentDirectory());

        foreach (var file in files)
        {
            if (file.EndsWith(".old") || file.EndsWith(".temp") || file.Equals(Consts.UpdateFile))
            {
                File.Delete(file);
            }
        }

        var updateDir = Path.Combine(Directory.GetCurrentDirectory(), Consts.UpdateFolder);

        if (Directory.Exists(updateDir))
        {
            Directory.Delete(updateDir, true);
        }
    }
}
