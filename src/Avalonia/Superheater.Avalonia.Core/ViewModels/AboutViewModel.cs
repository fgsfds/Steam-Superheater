using Common;
using Common.Helpers;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Superheater.Avalonia.Core.ViewModels
{
    internal sealed partial class AboutViewModel(AppUpdateInstaller updateInstaller) : ObservableObject
    {
        private readonly AppUpdateInstaller _updateInstaller = updateInstaller ?? ThrowHelper.ArgumentNullException<AppUpdateInstaller>(nameof(updateInstaller));


        #region Binding Properties

        public static Version CurrentVersion => CommonProperties.CurrentVersion;


        public string AboutTabHeader { get; private set; } = "About";

        public string CheckForUpdatesButtonText { get; private set; } = string.Empty;


        public bool IsUpdateAvailable { get; private set; }

        public bool IsInProgress { get; private set; }

        #endregion Binding Properties


        #region Relay Commands

        /// <summary>
        /// VM initialization
        /// </summary>
        [RelayCommand]
        private async Task InitializeAsync() => await CheckForUpdates();

        /// <summary>
        /// Check for SSH updates
        /// </summary>
        [RelayCommand(CanExecute = (nameof(CheckForUpdatesCanExecute)))]
        private async Task CheckForUpdates()
        {
            IsInProgress = true;
            OnPropertyChanged(nameof(IsInProgress));
            CheckForUpdatesCommand.NotifyCanExecuteChanged();

            bool updates = false;

            try
            {
                CheckForUpdatesButtonText = "Checking...";
                OnPropertyChanged(nameof(CheckForUpdatesButtonText));
                updates = await _updateInstaller.CheckForUpdates(CurrentVersion);
            }
            catch (Exception ex)
            {
                new PopupMessageViewModel(
                    "Error",
                    @$"Cannot retrieve latest releases from GitHub:
                    
{ex.Message}",
                    PopupMessageType.OkOnly
                    ).Show();
            }

            if (updates)
            {
                IsUpdateAvailable = true;
                OnPropertyChanged(nameof(IsUpdateAvailable));
                DownloadAndInstallCommand.NotifyCanExecuteChanged();

                UpdateHeader();
            }
            else
            {
                CheckForUpdatesButtonText = "Already up-to-date";
                OnPropertyChanged(nameof(CheckForUpdatesButtonText));
            }

            IsInProgress = false;
            OnPropertyChanged(nameof(IsInProgress));
            CheckForUpdatesCommand.NotifyCanExecuteChanged();
        }
        private bool CheckForUpdatesCanExecute() => IsInProgress is false;

        /// <summary>
        /// Download and install SSH update
        /// </summary>
        [RelayCommand(CanExecute = (nameof(DownloadAndInstallCanExecute)))]
        private async Task DownloadAndInstall()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                IsInProgress = true;
                OnPropertyChanged(nameof(IsInProgress));
                DownloadAndInstallCommand.NotifyCanExecuteChanged();

                await _updateInstaller.DownloadAndUnpackLatestRelease();

                AppUpdateInstaller.InstallUpdate();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://github.com/fgsfds/Steam-Superheater/releases",
                    UseShellExecute = true
                });
            }
            else
            {
                ThrowHelper.PlatformNotSupportedException("Can't identify platform");
            }


        }
        private bool DownloadAndInstallCanExecute() => IsUpdateAvailable is true;

        #endregion Relay Commands


        /// <summary>
        /// Update tab header
        /// </summary>
        private void UpdateHeader()
        {
            AboutTabHeader = "About" + (IsUpdateAvailable
                ? " (Update available)"
                : string.Empty);

            OnPropertyChanged(nameof(AboutTabHeader));
        }
    }
}