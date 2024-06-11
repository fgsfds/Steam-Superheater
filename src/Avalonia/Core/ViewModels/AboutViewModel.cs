using Common.Client;
using Common;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Superheater.Avalonia.Core.ViewModels
{
    public sealed partial class AboutViewModel : ObservableObject
    {
        private readonly AppUpdateInstaller _updateInstaller;
        private readonly Logger _logger;


        #region Binding Properties

        public Version CurrentVersion => ClientProperties.CurrentVersion;

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
        /// Check for SSH updates
        /// </summary>
        [RelayCommand(CanExecute = (nameof(CheckForUpdatesCanExecute)))]
        private async Task CheckForUpdatesAsync()
        {
            IsInProgress = true;

            CheckForUpdatesButtonText = "Checking...";

            var result = await _updateInstaller.CheckForUpdates(CurrentVersion).ConfigureAwait(true);

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

                UpdateHeader();
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

            await _updateInstaller.DownloadAndUnpackLatestRelease(new()).ConfigureAwait(true);

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