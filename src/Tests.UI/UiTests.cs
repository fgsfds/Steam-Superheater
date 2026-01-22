using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Common.Axiom;
using Common.Client.DI;
using Microsoft.Extensions.DependencyInjection;

namespace Tests.UI;

public sealed class UiTests : IDisposable
{
    public UiTests()
    {
        _ = Directory.CreateDirectory("ScreenshotsActual");
        _ = Directory.CreateDirectory("ScreenshotsFails");
    }

    public void Dispose()
    {
#if !DEBUG
        try
        {
            Directory.Delete("ScreenshotsActual", true);
        }
        catch { }
#endif
    }


    [AvaloniaTheory(Timeout = 30_000), Trait("Category", "UI")]
    [InlineData("StartupTest.png")]
    public async Task StartupTest(string screenshotName)
    {
        var window = Helpers.CreateWindow();

        var progress = window.GetDescendantWithName<ProgressBar>("GeneralProgressBar") ?? throw new ArgumentNullException();
        var progressText = window.GetDescendantWithName<TextBlock>("GeneralProgressBarText") ?? throw new ArgumentNullException();
        var cancelButton = window.GetDescendantWithName<Button>("CancelButton") ?? throw new ArgumentNullException();

        while (progress.IsIndeterminate)
        {
            Assert.Equal("Updating...", progressText.Text);
            Assert.True(cancelButton.IsVisible);
            Assert.True(cancelButton.IsEnabled);
            await Task.Delay(500);
        }

        Assert.Equal("", progressText.Text);
        Assert.False(cancelButton.IsVisible);

        await window.MakeScreenshotAsync("1_" + screenshotName);
        Assert.True(Helpers.CompareScreenshotsAndMoveFailed("1_" + screenshotName), "1_" + screenshotName);

        var config = BindingsManager.Provider.GetRequiredService<IConfigProvider>();
        config.ShowUninstalledGames = false;

        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        await Task.Delay(500);

        await window.MakeScreenshotAsync("2_" + screenshotName);
        Assert.True(Helpers.CompareScreenshotsAndMoveFailed("2_" + screenshotName), "2_" + screenshotName);
    }

    [AvaloniaTheory(Timeout = 30_000), Trait("Category", "UI")]
    [InlineData("SearchForGameTest.png")]
    public async Task SearchForGameTest(string screenshotName)
    {
        var window = Helpers.CreateWindow();

        var searchBox = window.GetDescendantWithName<TextBox>("SearchBox") ?? throw new ArgumentNullException();
        var gamesList = window.GetDescendantWithName<ListBox>("GamesListBox") ?? throw new ArgumentNullException();
        var progress = window.GetDescendantWithName<ProgressBar>("GeneralProgressBar") ?? throw new ArgumentNullException();
        var clearSearch = window.GetDescendantWithName<Button>("ClearSearchButton") ?? throw new ArgumentNullException();

        Assert.False(clearSearch.IsEffectivelyEnabled);

        while (progress.IsIndeterminate)
        {
            await Task.Delay(500);
        }

        _ = searchBox.Focus();
        window.KeyTextInput("  mAx pAYNe  ");

        Assert.Equal(3, gamesList.Items.Count);

        window.ClickMainTab();

        await window.MakeScreenshotAsync(screenshotName);

        Assert.True(Helpers.CompareScreenshotsAndMoveFailed(screenshotName), screenshotName);

        window.ClickClearSearchButton();

        Assert.True(gamesList.Items.Count > 100);
    }

    [AvaloniaTheory(Timeout = 30_000), Trait("Category", "UI")]
    [InlineData("SelectDependantFixTest.png")]
    public async Task SelectDependantFixTest(string screenshotName)
    {
        var window = Helpers.CreateWindow();

        var progress = window.GetDescendantWithName<ProgressBar>("GeneralProgressBar") ?? throw new ArgumentNullException();

        while (progress.IsIndeterminate)
        {
            await Task.Delay(500);
        }

        window.Click(50, 150);

        window.Click(400, 190);

        await window.MakeScreenshotAsync(screenshotName);

        Assert.True(Helpers.CompareScreenshotsAndMoveFailed(screenshotName), screenshotName);
    }

    [AvaloniaTheory(Timeout = 30_000), Trait("Category", "UI")]
    [InlineData("EditorTabTest.png")]
    public async Task EditorTabTest(string screenshotName)
    {
        var window = Helpers.CreateWindow();

        window.ClickEditorTab();
        //click refresh
        window.Click(290, 540);
        //click Shared
        window.Click(80, 140);
        //click Raze
        window.Click(430, 130);

        await window.MakeScreenshotAsync("1_" + screenshotName);
        Assert.True(Helpers.CompareScreenshotsAndMoveFailed("1_" + screenshotName), "1_" + screenshotName);

        //click Add new fix
        window.Click(360, 470);

        await window.MakeScreenshotAsync("2_" + screenshotName);

        Assert.True(Helpers.CompareScreenshotsAndMoveFailed("2_" + screenshotName), "2_" + screenshotName);
        //click Registry fix
        window.Click(750, 130);

        await window.MakeScreenshotAsync("3_" + screenshotName);
        Assert.True(Helpers.CompareScreenshotsAndMoveFailed("3_" + screenshotName), "3_" + screenshotName);

        //click Hosts fix
        window.Click(810, 130);

        await window.MakeScreenshotAsync("4_" + screenshotName);
        Assert.True(Helpers.CompareScreenshotsAndMoveFailed("4_" + screenshotName), "4_" + screenshotName);

        //click Text fix
        window.Click(930, 130);

        await window.MakeScreenshotAsync("5_" + screenshotName);
        Assert.True(Helpers.CompareScreenshotsAndMoveFailed("5_" + screenshotName), "5_" + screenshotName);
    }
}
