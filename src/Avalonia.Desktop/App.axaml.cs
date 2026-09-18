using Api.Client.DI;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Desktop.DI;
using Avalonia.Desktop.Helpers;
using Avalonia.Desktop.ViewModels;
using Avalonia.Desktop.Windows;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Common.Axiom;
using Common.Axiom.Enums;
using Common.Client;
using Common.Client.DI;
using Database.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Avalonia.Desktop;

/// <summary>
/// Application entry point and service configuration.
/// </summary>
public sealed class App : Application
{
    private static readonly Mutex _mutex = new(false, "Superheater");
    private static ILogger _logger = null!;
    private static App _app = null!;
    private static ServiceProvider _services = null!;

    /// <inheritdoc />
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        #if DEBUG
        this.AttachDeveloperTools();
        #endif

        _app = this;
    }

    /// <summary>
    /// Runs the application with the specified AppBuilder.
    /// </summary>
    /// <param name="builder">The application builder.</param>
    /// <returns>The application exit code.</returns>
    public static int Run(AppBuilder builder)
    {
        int code;

        using ClassicDesktopStyleApplicationLifetime lifetime = new()
        {
            ShutdownMode = ShutdownMode.OnMainWindowClose
        };

        _ = builder.SetupWithLifetime(lifetime);

        LoadBindings();

        _logger = _services.GetRequiredService<ILogger>();

        //run after setting _logger but before initializing anything else!
        Cleanup();

        using (var dbContext = _services.GetRequiredService<IDbContextFactory<DatabaseContext>>().CreateDbContext())
        {
            dbContext.Database.Migrate();
        }

        var config = _services.GetRequiredService<IConfigProvider>();
        var viewModelsFactory = _services.GetRequiredService<IViewModelsFactory>();

        SetTheme(config.Theme);

        lifetime.MainWindow = new MainWindow(viewModelsFactory);
        lifetime.MainWindow.DataContext = viewModelsFactory.GetMainWindowViewModel();

        //initialize
        _ = NotificationsHelper.NotificationManager;

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
    /// Builds the service provider and loads the DI bindings.
    /// </summary>
    public static void LoadBindings()
    {
        ServiceCollection services = new();

        _ = services.WithLogging(Design.IsDesignMode);
        _ = services.WithCommon();
        _ = services.WithDatabase();
        _ = services.WithProviders(Design.IsDesignMode);
        _ = services.WithModels();
        _ = services.WithViewModels();
        _ = services.WithApi();

        _services?.Dispose();

        _services = services.BuildServiceProvider(
            new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true
            });
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
            _ => throw new ArgumentOutOfRangeException(theme.ToString())
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
                || file.Equals(ClientConstants.UpdateFile, StringComparison.OrdinalIgnoreCase)
                || file.EndsWith(".db-wal", StringComparison.OrdinalIgnoreCase)
                || file.EndsWith(".db-shm", StringComparison.OrdinalIgnoreCase))
            {
                File.Delete(file);
            }
        }

        var updateDir = Path.Combine(ClientProperties.WorkingFolder, ClientConstants.UpdateFolder);

        if (Directory.Exists(updateDir))
        {
            Directory.Delete(updateDir, true);
        }
    }
}
