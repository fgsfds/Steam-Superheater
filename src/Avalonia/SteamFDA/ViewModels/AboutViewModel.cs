using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SteamFDA.Helpers;
using SteamFDCommon;
using SteamFDCommon.Helpers;
using System;
using System.Threading.Tasks;

namespace SteamFDA.ViewModels
{
    internal partial class AboutViewModel : ObservableObject
    {
        private readonly UpdateInstaller _updateInstaller;

        public bool IsUpdateAvailable { get; set; }

        public bool IsInProgress { get; set; }

        public Version CurrentVersion => CommonProperties.CurrentVersion;

        public AboutViewModel(UpdateInstaller updateInstaller)
        {
            _updateInstaller = updateInstaller ?? throw new NullReferenceException(nameof(updateInstaller));
        }


        #region Relay Commands

        [RelayCommand(CanExecute = (nameof(CheckForUpdatesCanExecute)))]
        private async Task CheckForUpdates()
        {
            IsInProgress = true;
            OnPropertyChanged(nameof(IsInProgress));
            CheckForUpdatesCommand.NotifyCanExecuteChanged();

            bool updates = false;

            try
            {
                updates = await _updateInstaller.CheckForUpdates(CurrentVersion);
            }
            catch (Exception ex)
            {
                new PopupMessageViewModel(
                    "Error",
                    "Cannot retreive latest releases from GitHub:" + Environment.NewLine + ex.Message,
                    PopupMessageType.OkOnly
                    ).Show();
            }

            if (updates)
            {
                IsUpdateAvailable = true;
                OnPropertyChanged(nameof(IsUpdateAvailable));
                DownloadAndInstallCommand.NotifyCanExecuteChanged();
            }

            IsInProgress = false;
            OnPropertyChanged(nameof(IsInProgress));
            CheckForUpdatesCommand.NotifyCanExecuteChanged();
        }
        private bool CheckForUpdatesCanExecute() => IsInProgress is false;


        [RelayCommand(CanExecute = (nameof(DownloadAndInstallCanExecute)))]
        private async Task DownloadAndInstall()
        {
            IsInProgress = true;
            OnPropertyChanged(nameof(IsInProgress));
            DownloadAndInstallCommand.NotifyCanExecuteChanged();

            await _updateInstaller.DownloadLatestReleaseAndCreateLock();

            UpdateInstaller.InstallUpdate();

            FdaProperties.MainWindow.Close();
        }
        private bool DownloadAndInstallCanExecute() => IsUpdateAvailable is true;

        #endregion Relay Commands
    }
}