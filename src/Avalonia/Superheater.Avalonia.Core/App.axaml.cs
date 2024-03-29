﻿using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Common;
using Common.Config;
using Common.DI;
using Common.Enums;
using Common.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Superheater.Avalonia.Core.DI;
using Superheater.Avalonia.Core.Helpers;
using Superheater.Avalonia.Core.Pages;
using Superheater.Avalonia.Core.Windows;

namespace Superheater.Avalonia.Core;

public sealed class App : Application
{
    private static readonly Mutex _mutex = new (false, "Superheater");

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
            ProvidersBindings.Load(container);

            var theme = BindingsManager.Provider.GetRequiredService<ConfigProvider>().Config.Theme;

            var themeEnum = theme switch
            {
                ThemeEnum.System => ThemeVariant.Default,
                ThemeEnum.Light => ThemeVariant.Light,
                ThemeEnum.Dark => ThemeVariant.Dark,
                _ => ThrowHelper.ArgumentOutOfRangeException<ThemeVariant>(theme.ToString())
            };

            RequestedThemeVariant = themeEnum;

            if (Properties.IsDeveloperMode)
            {
                Logger.Info("Started in developer mode");
            }

            Cleanup();

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
        catch (Exception ex)
        {
            var messageBox = new MessageBox(ex.Message);
            messageBox.Show();

            Logger.Error(ex.ToString());

            if (!Properties.IsDeveloperMode)
            {
                Logger.UploadLog();
            }
        }
    }

    /// <summary>
    /// Check if you can write to the current directory
    /// </summary>
    private static bool DoesHaveWriteAccess(string folderPath)
    {
        try
        {
            FileStream fs = File.Create(Path.Combine(folderPath, Path.GetRandomFileName()));
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
        Logger.Info("Starting cleanup");

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
