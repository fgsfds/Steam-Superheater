using Avalonia;
using Avalonia.Controls;
using Avalonia.Desktop.Windows;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.VisualTree;
using Codeuctivity.ImageSharpCompare;
using Common.Axiom;
using Common.Client;
using Common.Client.DI;
using Microsoft.Extensions.DependencyInjection;

namespace Tests.UI;

internal static class Helpers
{
    /// <summary>
    /// Creates app window and sets initial settings.
    /// </summary>
    internal static MainWindow CreateWindow()
    {
        ClientProperties.IsOfflineMode = true;

        var config = BindingsManager.Provider.GetRequiredService<IConfigProvider>();

        config.ShowUninstalledGames = true;
        config.ShowUnsupportedFixes = true;
        config.LastReadNewsDate = DateTime.MaxValue;
        config.IsConsented = true;

        var window = new MainWindow();
        window.Height = 600;
        window.Width = 1000;
        window.Show();

        return window;
    }

    /// <summary>
    /// Checks if two images in Screenshots and ScreenshotsActual folders are equal.
    /// </summary>
    /// <param name="imageName">Name of the image.</param>
    internal static bool AreImagesEqual(string imageName)
    {
        var diff = ImageSharpCompare.CalcDiff(
            Path.Combine("ScreenshotsActual", imageName),
            Path.Combine("Screenshots", imageName)
            );

        return diff.PixelErrorCount < 10;
    }

    internal static bool CompareScreenshotsAndMoveFailed(string screenshotName)
    {
        var areEqual = AreImagesEqual(screenshotName);

        if (areEqual)
        {
            return true;
        }

        var from = Path.Combine("ScreenshotsActual", screenshotName);
        var to = Path.Combine("ScreenshotsFails", screenshotName);

        File.Move(from, to, true);

        return false;
    }
}

internal static class Clicker
{
    /// <summary>
    /// Clicks left mouse button at coordinates.
    /// </summary>
    /// <param name="window">Main window.</param>
    /// <param name="x">X coordinate.</param>
    /// <param name="y">Y coordinate.</param>
    public static void Click(this MainWindow window, int x, int y)
    {
        Point point = new(x, y);
        window.MouseDown(point, MouseButton.Left);
        window.MouseUp(point, MouseButton.Left);
    }

    public static void ClickMainTab(this MainWindow window) => window.Click(50, 50);

    public static void ClickEditorTab(this MainWindow window) => window.Click(130, 50);

    public static void ClickNewsTab(this MainWindow window) => window.Click(220, 50);

    public static void ClickSettingsTab(this MainWindow window) => window.Click(320, 50);

    public static void ClickAboutTab(this MainWindow window) => window.Click(430, 50);

    public static void ClickClearSearchButton(this MainWindow window) => window.Click(960, 95);
}

internal static class Extensions
{
    /// <summary>
    /// Gets descendant of type <see langword="T"/> and with specific name.
    /// </summary>
    /// <typeparam name="T">Type.</typeparam>
    /// <param name="window">Main window.</param>
    /// <param name="name">Descendant name.</param>
    public static T? GetDescendantWithName<T>(this Window window, string name) where T : Control
    {
        return window.GetVisualDescendants().FirstOrDefault(x => x is INamed tc && (tc.Name?.Equals(name) ?? false)) as T;
    }

    /// <summary>
    /// Creates screenshot at ScreenshotsActual folder.
    /// </summary>
    /// <param name="window">Main window.</param>
    /// <param name="screnshotName">Name of the screenshot.</param>
    public static async Task MakeScreenshotAsync(this Window window, string screnshotName)
    {
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        await Task.Delay(500);

        using var frame = window.CaptureRenderedFrame() ?? throw new ArgumentNullException();
        frame.Save(Path.Combine("ScreenshotsActual", screnshotName));
    }
}

internal sealed class SteamToolsStub : ISteamTools
{
    /// <inheritdoc/>
    public string? SteamInstallPath => null;

    /// <inheritdoc/>
    public List<string> GetAcfsList() => [Path.Combine("ACFs", "appmanifest_3929740.acf")];
}