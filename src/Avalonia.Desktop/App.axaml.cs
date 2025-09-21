using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Desktop.DI;
using Avalonia.Desktop.ViewModels;
using Avalonia.Desktop.Windows;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Common;
using Common.Client;
using Common.Client.DI;
using Common.Enums;
using Common.Helpers;
using CommunityToolkit.Diagnostics;
using Database.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Avalonia.Desktop;

public sealed class App : Application
{
    private static readonly Mutex _mutex = new(false, "Superheater");
    private static ILogger _logger = null!;
    private static App _app = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        _app = this;
    }

    public static int Run(AppBuilder builder)
    {
        int code;

        using ClassicDesktopStyleApplicationLifetime lifetime = new()
        {
            ShutdownMode = ShutdownMode.OnMainWindowClose
        };

        _ = builder.SetupWithLifetime(lifetime);

        LoadBindings();

        _logger = BindingsManager.Provider.GetRequiredService<ILogger>();

        //run after setting _logger but before initializing anything else!
        Cleanup();

        var config = BindingsManager.Provider.GetRequiredService<IConfigProvider>();
        var mainViewModel = BindingsManager.Provider.GetRequiredService<MainWindowViewModel>();

        SetTheme(config.Theme);

        lifetime.MainWindow = new MainWindow();
        lifetime.MainWindow.DataContext = mainViewModel;

        if (ClientProperties.IsDeveloperMode)
        {
            _logger.LogInformation("Started in developer mode");
        }

        if (ClientProperties.IsOfflineMode)
        {
            _logger.LogInformation("Starting in offline mode");
        }

        if (ClientProperties.IsInSteamDeckGameMode)
        {
            _logger.LogInformation("Starting in Steam Deck mode");
        }

        _logger.LogInformation($"Superheater version: {ClientProperties.CurrentVersion}");
        _logger.LogInformation($"Operating system: {Environment.OSVersion}");
        _logger.LogInformation($"Working folder is {ClientProperties.WorkingFolder}");

        if (!Design.IsDesignMode)
        {
            if (!DoesHaveWriteAccess(ClientProperties.WorkingFolder))
            {
                var messageBox = new MessageBox($"""
                Superheater doesn't have write access to
                {ClientProperties.WorkingFolder}
                and can't be launched. 
                Move it to the folder where you have write access.
                """);
                messageBox.Show();
                return -1;
            }

            if (!_mutex.WaitOne(1000, false))
            {
                var messageBox = new MessageBox("You can't launch multiple instances of Superheater");
                messageBox.Show();
                return -1;
            }
        }

        try
        {
            code = lifetime.Start();
        }
        catch (Exception ex)
        {
            _logger?.LogCritical(ex, "== Critical error while running app ==");

            try
            {
                lifetime.Shutdown();
            }
            catch (Exception ex2)
            {
                _logger?.LogCritical(ex2, "== Critical error while shutting down app ==");
            }

            throw;
        }

        return code;
    }

    /// <summary>
    /// Load DI bindings
    /// </summary>
    private static void LoadBindings()
    {
        var container = BindingsManager.Instance;

        ModelsBindings.Load(container);
        ViewModelsBindings.Load(container);
        CommonBindings.Load(container, Design.IsDesignMode);
        ProvidersBindings.Load(container, Design.IsDesignMode);
    }

    /// <summary>
    /// Set theme from the config
    /// </summary>
    private static void SetTheme(ThemeEnum theme)
    {
        var themeEnum = theme switch
        {
            ThemeEnum.System => ThemeVariant.Default,
            ThemeEnum.Light => ThemeVariant.Light,
            ThemeEnum.Dark => ThemeVariant.Dark,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<ThemeVariant>(theme.ToString())
        };

        _app.RequestedThemeVariant = themeEnum;
    }

    /// <summary>
    /// Check if you can write to the current directory
    /// </summary>
    private static bool DoesHaveWriteAccess(string folderPath)
    {
        try
        {
            using var fs = File.Create(Path.Combine(folderPath, Path.GetRandomFileName()));
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
        if (Design.IsDesignMode)
        {
            return;
        }

        _logger?.LogInformation("Starting cleanup");

        var files = Directory.GetFiles(ClientProperties.WorkingFolder);

        foreach (var file in files)
        {
            if (file.EndsWith(".old", StringComparison.OrdinalIgnoreCase)
                || file.EndsWith(".temp", StringComparison.OrdinalIgnoreCase)
                || file.Equals(Consts.UpdateFile, StringComparison.OrdinalIgnoreCase)
                || file.EndsWith(".db-wal", StringComparison.OrdinalIgnoreCase)
                || file.EndsWith(".db-shm", StringComparison.OrdinalIgnoreCase))
            {
                File.Delete(file);
            }
        }

        var updateDir = Path.Combine(ClientProperties.WorkingFolder, Consts.UpdateFolder);

        if (Directory.Exists(updateDir))
        {
            Directory.Delete(updateDir, true);
        }
    }
}