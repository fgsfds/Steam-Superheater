using Common;
using Common.Helpers;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Superheater.Avalonia.Core.ViewModels.Popups;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Superheater.Avalonia.Core.ViewModels
{
    public sealed partial class AboutViewModel(
        AppUpdateInstaller updateInstaller,
        PopupMessageViewModel popupMessage
        ) : ObservableObject
    {
        private readonly AppUpdateInstaller _updateInstaller = updateInstaller;
        private readonly PopupMessageViewModel _popupMessage = popupMessage;


        #region Binding Properties

        public Version CurrentVersion => CommonProperties.CurrentVersion;

        [ObservableProperty]
        private string _aboutTabHeader = "About";

        [ObservableProperty]
        private string _checkForUpdatesButtonText = string.Empty;

        [ObservableProperty]
        private bool _isUpdateAvailable;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(CheckForUpdatesCommand))]
        [NotifyCanExecuteChangedFor(nameof(DownloadAndInstallCommand))]
        private bool _isInProgress;

        #endregion Binding Properties


        #region Relay Commands

        /// <summary>
        /// VM initialization
        /// </summary>
        [RelayCommand]
        private Task InitializeAsync() => CheckForUpdatesAsync();

        /// <summary>
        /// Check for SSH updates
        /// </summary>
        [RelayCommand(CanExecute = (nameof(CheckForUpdatesCanExecute)))]
        private async Task CheckForUpdatesAsync()
        {
            IsInProgress = true;

            var updates = false;

            try
            {
                CheckForUpdatesButtonText = "Checking...";
                updates = await _updateInstaller.CheckForUpdates(CurrentVersion).ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                var message = $"""
                               Cannot retrieve latest releases from GitHub:
                                                   
                               {ex.Message}
                               """;

                _popupMessage.Show(
                    "Error",
                    message,
                    PopupMessageType.OkOnly
                    );

                Logger.Error(message);
            }

            if (updates)
            {
                IsUpdateAvailable = true;

                UpdateHeader();
            }
            else
            {
                CheckForUpdatesButtonText = "Already up-to-date";
            }

            IsInProgress = false;
        }
        private bool CheckForUpdatesCanExecute() => IsInProgress is false;

        /// <summary>
        /// Download and install SSH update
        /// </summary>
        [RelayCommand(CanExecute = (nameof(DownloadAndInstallCanExecute)))]
        private async Task DownloadAndInstallAsync()
        {
            IsInProgress = true;

            await _updateInstaller.DownloadAndUnpackLatestRelease().ConfigureAwait(true);

            AppUpdateInstaller.InstallUpdate();
        }
        private bool DownloadAndInstallCanExecute() => IsUpdateAvailable;

        #endregion Relay Commands


        /// <summary>
        /// Update tab header
        /// </summary>
        private void UpdateHeader()
        {
            AboutTabHeader = "About" + (IsUpdateAvailable
                ? " (Update available)"
                : string.Empty);
        }
    }
}