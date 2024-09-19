using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Notifications;
using Avalonia.Data.Core.Plugins;
using Avalonia.Desktop.DI;
using Avalonia.Desktop.Helpers;
using Avalonia.Desktop.Windows;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Common;
using Common.Client;
using Common.Client.DI;
using Common.Client.Logger;
using Common.Enums;
using Common.Helpers;
using CommunityToolkit.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Avalonia.Desktop;

public sealed class App : Application
{
    public static WindowNotificationManager NotificationManager { get; private set; }
    public static Random Random { get; private set; }

    private static readonly Mutex _mutex = new(false, "Superheater");
    private static ILogger? _logger = null;

    static App()
    {
        //initialized later
        NotificationManager = null!;
        Random = null!;
    }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ClientProperties.HasCrashed is not null && ClientProperties.HasCrashed.Item1 && !Design.IsDesignMode)
        {
            var messageBox = new MessageBox(ClientProperties.HasCrashed.Item2);
            messageBox.Show();
            return;
        }

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
                return;
            }

            if (!_mutex.WaitOne(1000, false))
            {
                var messageBox = new MessageBox($"You can't launch multiple instances of Superheater");
                messageBox.Show();
                return;
            }
        }

        try
        {
            var container = BindingsManager.Instance;

            ModelsBindings.Load(container);
            ViewModelsBindings.Load(container);
            CommonBindings.Load(container, Design.IsDesignMode);
            ProvidersBindings.Load(container, Design.IsDesignMode);

            var theme = BindingsManager.Provider.GetRequiredService<IConfigProvider>().Theme;
            _logger = BindingsManager.Provider.GetRequiredService<ILogger>();

            var themeEnum = theme switch
            {
                ThemeEnum.System => ThemeVariant.Default,
                ThemeEnum.Light => ThemeVariant.Light,
                ThemeEnum.Dark => ThemeVariant.Dark,
                _ => ThrowHelper.ThrowArgumentOutOfRangeException<ThemeVariant>(theme.ToString())
            };

            RequestedThemeVariant = themeEnum;

            if (ClientProperties.IsDeveloperMode)
            {
                _logger.Info("Started in developer mode");
            }

            _logger.Info($"Working folder is {ClientProperties.WorkingFolder}");

            Cleanup();

            // Line below is needed to remove Avalonia data validation.
            // Without this line you will get duplicate validations from both Avalonia and CT
            BindingPlugins.DataValidators.RemoveAt(0);

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow();
                desktop.Exit += OnAppExit;
            }

            NotificationManager = new(AvaloniaProperties.TopLevel)
            {
                MaxItems = 3,
                Position = NotificationPosition.TopRight,
                Margin = new(0, 50, 10, 0)
            };

            Random = new();

            base.OnFrameworkInitializationCompleted();
        }
        catch (Exception ex)
        {
            if (Design.IsDesignMode)
            {
                return;
            }

            var messageBox = new MessageBox(ex.ToString());
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
        if (Design.IsDesignMode)
        {
            return;
        }

        _logger?.Info("Starting cleanup");

        var files = Directory.GetFiles(ClientProperties.WorkingFolder);

        foreach (var file in files)
        {
            if (file.EndsWith(".old") || file.EndsWith(".temp") || file.Equals(Consts.UpdateFile))
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