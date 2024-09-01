using Common;
using Common.Client;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Avalonia.Core.ViewModels;

public sealed partial class AboutViewModel : ObservableObject
{
    private readonly AppUpdateInstaller _updateInstaller;
    private readonly Logger _logger;


    #region Binding Properties

    /// <summary>
    /// Tab header
    /// </summary>
    public string AboutTabHeader =>
        "About" + (IsUpdateAvailable
            ? " (Update available)"
            : string.Empty);

    /// <summary>
    /// Text on the check for updates button
    /// </summary>
    [ObservableProperty]
    private string _checkForUpdatesButtonText = string.Empty;

    /// <summary>
    /// Is app update available
    /// </summary>
    [ObservableProperty]
    private bool _isUpdateAvailable;

    /// <summary>
    /// Is some operation in progress
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CheckForUpdatesCommand))]
    [NotifyCanExecuteChangedFor(nameof(DownloadAndInstallCommand))]
    private bool _isInProgress;

    #endregion Binding Properties


    public AboutViewModel(
        AppUpdateInstaller updateInstaller,
        Logger logger)
    {
        _updateInstaller = updateInstaller;
        _logger = logger;
    }


    #region Relay Commands

    /// <summary>
    /// VM initialization
    /// </summary>
    [RelayCommand]
    private Task InitializeAsync() => CheckForUpdatesAsync();

    /// <summary>
    /// Check for the app updates
    /// </summary>
    [RelayCommand(CanExecute = (nameof(CheckForUpdatesCanExecute)))]
    private async Task CheckForUpdatesAsync()
    {
        IsInProgress = true;

        CheckForUpdatesButtonText = "Checking...";

        var result = await _updateInstaller.CheckForUpdates(ClientProperties.CurrentVersion).ConfigureAwait(true);

        if (result == ResultEnum.NotFound)
        {
            CheckForUpdatesButtonText = "Already up-to-date";
        }
        else if (!result.IsSuccess)
        {
            _logger.Error(result.Message);

            CheckForUpdatesButtonText = result.Message;
        }
        else
        {
            IsUpdateAvailable = true;

            OnPropertyChanged(nameof(AboutTabHeader));
        }

        IsInProgress = false;
    }
    private bool CheckForUpdatesCanExecute() => IsInProgress is false;

    /// <summary>
    /// Download and install app update
    /// </summary>
    [RelayCommand(CanExecute = (nameof(DownloadAndInstallCanExecute)))]
    private async Task DownloadAndInstallAsync()
    {
        IsInProgress = true;

        await _updateInstaller.DownloadAndUnpackLatestRelease(new()).ConfigureAwait(true);

        AppUpdateInstaller.InstallUpdate();
    }
    private bool DownloadAndInstallCanExecute() => IsUpdateAvailable;

    #endregion Relay Commands
}

